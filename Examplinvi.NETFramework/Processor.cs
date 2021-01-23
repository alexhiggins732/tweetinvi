using Dapper;
using Examplinvi.DbFx;
using Examplinvi.DbFx.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Tweetinvi;
using Tweetinvi.Core.Factories;
using Tweetinvi.Events;
using Tweetinvi.Logic.DTO;
using Tweetinvi.Models;
using Tweetinvi.Streaming;

namespace Examplinvi.NETFramework
{
    public class Monitored : TweetDTO
    {
        private Monitor monitor;
        private ITweet tweet;

        public ConcurrentDictionary<long, Monitored> Replies;
        public ConcurrentDictionary<long, Monitored> QuotedBy;
        public ConcurrentDictionary<long, Monitored> Retweets;

        public Monitored RepliedToTweet = null;
        public Monitored QuotedTweet = null;
        public Monitored RetweedTweet = null;
        public Monitored(Monitor monitor, ITweet tweet)
        {
            Replies = new ConcurrentDictionary<long, Monitored>();
            QuotedBy = new ConcurrentDictionary<long, Monitored>();
            Retweets = new ConcurrentDictionary<long, Monitored>();


            this.monitor = monitor;
            this.tweet = tweet;
            Func<long?, Monitored> getOrAdd = (sourceId) =>
                monitor.monitored.GetOrAdd((long)sourceId, (id) => new Monitored(monitor, null));

            if (tweet != null)
            {
                var tweetId = tweet.Id;
                if (tweet.InReplyToStatusId.HasValue)
                {
                    var RepliedToTweet = getOrAdd(tweet.InReplyToStatusId); ///monitor.monitored.GetOrAdd(replyToId, (id) => new Monitored(monitor, null));
                    RepliedToTweet.Replies.TryAdd(tweetId, this);
                }
                if (tweet.QuotedStatusId.HasValue)
                {
                    QuotedTweet = getOrAdd(tweet.QuotedStatusId); //monitor.monitored.GetOrAdd(quotedId, (id) => new Monitored(monitor, null));
                    QuotedTweet.QuotedBy.TryAdd(tweetId, this);
                }
                if (tweet.IsRetweet)
                {
                    var RetweedTweet = getOrAdd(tweet.RetweetedTweet.Id);// monitor.monitored.GetOrAdd(retweetedId, (id) => new Monitored(monitor, null));
                    RetweedTweet.Retweets.TryAdd(tweetId, this);
                }
            }

        }
    }
    public class Monitor
    {
        public ConcurrentDictionary<long, Monitored> monitored;
        private ITweet tweet;



        public void Add(ITweet tweet)
        {
            var m = monitored.GetOrAdd(tweet.Id, (id) => new Monitored(this, tweet));
        }

    }

    public class DbLockResult
    {
        public Guid LockId { get; set; }
        public bool LockResult { get; set; }
    }
    public class DbLockHelper
    {
        internal static DbLockResult GetDbLock(SqlConnection conn)
        {
            int timeout = 100;
            int maxTimeOut = 10000;
            var ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var frame = new System.Diagnostics.StackFrame(1);
            var type = typeof(DbLockHelper).GetType();
            var LockReason = frame.GetMethod().Name;
            var AppName = frame.GetMethod().DeclaringType.FullName;

            var result = conn.QueryFirstOrDefault<DbLockResult>("SP_GetDbLock", new { ThreadId, AppName, LockReason }, commandType: CommandType.StoredProcedure);
            while (!result.LockResult)
            {
                timeout <<= 1;
                if (timeout > maxTimeOut) timeout = maxTimeOut;
                System.Threading.Thread.Sleep(timeout);
                result = conn.QueryFirstOrDefault<DbLockResult>("SP_GetDbLock", new { ThreadId, AppName, LockReason }, commandType: CommandType.StoredProcedure);


                var l = new DbLockResult { LockId = Guid.NewGuid() };

            }
            return result;
        }

        internal static DbLockResult ReleaseLock(SqlConnection conn, DbLockResult dbLock)
        {
            return conn.QueryFirst<DbLockResult>("Sp_ReleaseLock", new { dbLock.LockId }, commandType: CommandType.StoredProcedure);
        }
    }
    public class MetricProcessor
    {
        public DateTime LastStartDate;
        private bool canceled;
        public IFilteredStream stream;
        public Timer timer;
        ConcurrentQueue<ITweet> EnsureQueue = new ConcurrentQueue<ITweet>();
        ConcurrentQueue<ITweet> FollowQueue = new ConcurrentQueue<ITweet>();
        ConcurrentQueue<Metric> metrics = new ConcurrentQueue<Metric>();
        //public int ReceivedCount => receivedCount;
        //public int ReceivedOriginalCount => receivedOriginalCount;
        //public int FilteredCount => filteredCount;
        //ConcurrentQueue<ITweet> tweets = new ConcurrentQueue<ITweet>();
        IAuthenticatedUser currentUser;
        List<long> ids;
        ConcurrentBag<long> myTweetIds = new ConcurrentBag<long>();


        public MetricProcessor()
        {
            if (System.IO.File.Exists("AFQueue.json"))
            {
                var json = System.IO.File.ReadAllText("AFQueue.Json");
                var factory = TweetinviContainer.Resolve<ITweetFactory>();
                var items = JsonSerializer.ConvertJsonTo<List<TweetDTO>>(json);
                foreach (var item in items)
                {

                    var tweet = factory.GenerateTweetFromDTO(item);
                    FollowQueue.Enqueue(tweet);
                }
            }
        }
        public void Start()
        {
            canceled = false;
            stream = Stream.CreateFilteredStream();
            LogDebug("Processor.Start()");
            if (timer == null)
            {
                timer = new Timer();
                timer.AutoReset = true;
                timer.Interval = 10 * 1000;
                timer.Elapsed += Timer_Elapsed;
            }

            metrics = metrics ?? new ConcurrentQueue<Metric>();
            currentUser = currentUser ?? User.GetAuthenticatedUser();
            LogDebug("Retrieving Ids");
            var sw = Stopwatch.StartNew();
            if (ids == null)
            {
                ids = GetMonitorIds();
                if (!ids.Contains(currentUser.Id))
                {
                    throw new Exception("Self-monitoring check failed");
                }
            }
            LogDebug($"Retrieved {ids.Count} in {sw.Elapsed}");

            ids.ForEach(id => stream.AddFollow(id));
            stream.StreamStarted += Stream_StreamStarted;
            stream.StreamStopped += Stream_StreamStopped;
            stream.TweetDeleted += Stream_TweetDeleted;
            stream.DisconnectMessageReceived += Stream_DisconnectMessageReceived;
            stream.LimitReached += Stream_LimitReached;
            stream.MatchingTweetReceived += Stream_MatchingTweetReceived;
            stream.NonMatchingTweetReceived += Stream_NonMatchingTweetReceived;
            LogDebug($"Starting Stream");
            timer.Start();
            stream.StartStreamMatchingAnyCondition();
        }

        private List<long> GetMonitorIds()
        {
            List<long> ids = new List<long>();


            using (var repo = new TDbContext())
            {
                var users = repo.Users.Where(x => x.Following).OrderByDescending(x => x.FollowersCount).ToList();
                ids = users.Select(x => x.Id).Take(4000).ToList();
            }
            ids.Insert(0, currentUser.Id);
            if (ids.Count < 4000)
            {
                var apiIds = currentUser.GetFriendIds().Except(ids).Distinct().ToList();
                var take = 4000 - ids.Count;
                ids.AddRange(apiIds.Take(take));
            }
            return ids;
        }

        volatile bool saving = false;
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {

            LogDebug($"{nameof(Timer_Elapsed)} - Saving: {saving}");
            if (saving)
            {
                return;
            }
            saving = true;


            List<Metric> adds = new List<Metric>();
            List<long> deletedIds = new List<long>();
            int metricCount = metrics.Count;
            LogDebug($"Dequeueing {metricCount} metrics");
            while (metrics.Count > 0)
            {
                if (metrics.TryDequeue(out Metric result))
                {
                    if (result.Deleted)
                    {
                        deletedIds.Add(result.TweetId);
                    }
                    else
                    {
                        adds.Add(result);
                    }
                }
            }
            bool saved = false;
            int timeout = 1000;
            int maxTimeout = 30000;
            var sw = Stopwatch.StartNew();

            DbLockResult dbLock = null;
            int retryCount = 0;
            while (!saved)
            {
                try
                {
                    using (var repo = new TDbContext())
                    {
                        LogDebug($"Saving {adds.Count} metrics");
                        dbLock = dbLock ?? DbLockHelper.GetDbLock((SqlConnection)repo.Database.Connection);

                        var dt = new DataTable();
                        dt.Columns.Add("Id");
                        dt.Columns.Add("UserId");
                        dt.Columns.Add("TweetId");
                        dt.Columns.Add("RetweetId");
                        dt.Columns.Add("QuotedTweetId");
                        dt.Columns.Add("Deleted");
                        dt.Columns.Add("CreatedAt");
                        dt.Columns.Add("ReplyToTweetId");
                        dt.Columns.Add("Url");
                        for (var i = 0; i < adds.Count; i++)
                        {
                            var add = adds[i];
                            var row = dt.NewRow();
                            row[nameof(Metric.UserId)] = add.UserId;
                            row[nameof(Metric.TweetId)] = add.TweetId;
                            row[nameof(Metric.RetweetId)] = add.RetweetId;
                            row[nameof(Metric.QuotedTweetId)] = add.QuotedTweetId;
                            row[nameof(Metric.Deleted)] = add.Deleted;
                            row[nameof(Metric.CreatedAt)] = add.CreatedAt;
                            row[nameof(Metric.ReplyToTweetId)] = add.ReplyToTweetId;
                            row[nameof(Metric.Url)] = add.Url;
                            dt.Rows.Add(row);
                        }
                        using (var conn = new SqlConnection("server=.;initial catalog=tdb;trusted_connection=true"))
                        {
                            conn.Open();
                            using (var sqlBulk = new SqlBulkCopy(conn))
                            {
                                sqlBulk.DestinationTableName = "Metrics";
                                sqlBulk.WriteToServer(dt);
                                conn.Close();
                            }
                        }
                        saved = true;
                        DbLockHelper.ReleaseLock((SqlConnection)repo.Database.Connection, dbLock);
                    }
                    LogDebug($"Saved {adds.Count} metrics in {sw.Elapsed} - Last = {adds.Last().TweetId}");
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= 3)
                    {
                        using (var conn = new SqlConnection("server=.;initial catalog=tdb;trusted_connection=true"))
                        {
                            conn.Execute("sp_updatestats", commandType: CommandType.StoredProcedure, commandTimeout: 960);
                            conn.Execute("CleanupExpired2", commandType: CommandType.StoredProcedure, commandTimeout: 960);
                            conn.Execute("sp_updatestats", commandType: CommandType.StoredProcedure, commandTimeout: 960);
                            conn.Close();
                        }

                    }
                    LogDebug($"Error saving metrics: {ex.Message}");
                    System.Threading.Thread.Sleep(timeout);
                    timeout = Math.Min(timeout << 1, maxTimeout);
                }
            }
            foreach (var m in metrics)
            {
                if (m.UserId == currentUser.Id)
                {
                    LogDebug($"Received {m.TweetId}");
                }
            }
            dbLock = null;
            saved = false;
            timeout = 1000;
            sw.Restart();
            while (!saved)
            {
                LogDebug($"Checking {deletedIds.Count} deletes");
                if (deletedIds.Count == 0) saved = true;
                else
                {
                    try
                    {

                        using (var repo = new TDbContext())
                        {
                            dbLock = dbLock ?? DbLockHelper.GetDbLock((SqlConnection)repo.Database.Connection);
                            sw.Restart();
                            var deleted = repo.Metrics.Where(x => deletedIds.Contains(x.TweetId)).ToList();
                            LogDebug($"Found {deleted.Count} deletes in db in {sw.Elapsed}");
                            sw.Restart();
                            deleted.ForEach(x =>
                            {
                                x.Deleted = true;
                                repo.Metrics.Attach(x);
                                repo.Entry(x).State = System.Data.Entity.EntityState.Modified;
                            });
                            int updated = repo.SaveChanges();
                            LogDebug($"Marked {updated} deletes in db in {sw.Elapsed}");
                            saved = true;
                            DbLockHelper.ReleaseLock((SqlConnection)repo.Database.Connection, dbLock);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error marking metrics deleted: {ex.Message}");
                        System.Threading.Thread.Sleep(timeout);
                        timeout = Math.Min(timeout << 1, maxTimeout);
                    }

                }
            }
            dbLock = null;
            ProcessEnsures();
            saving = false;
            Task.Run(() => ProcesssFollows());
        }

        bool processingFollows = false;
        private void ProcesssFollows()
        {
            if (processingFollows) return;
            processingFollows = true;
            var sw = Stopwatch.StartNew();
            int followCount = FollowQueue.Count;
            LogDebug($"Following {followCount} ");

            while (FollowQueue.Count > 0)
            {
                if (FollowQueue.TryDequeue(out ITweet tweet))
                {
                    using (var repo = new TDbContext())
                    {
                        var followingInDb = repo.Database.Connection.QueryFirst<bool>("select cast(Isnull((select top 1 following from dbusers where id=@Id), 0) as bit)", new { tweet.CreatedBy.Id });
                        if (followingInDb)
                        {
                            LogDebug($"Autofollow: Already following {tweet.CreatedBy.ScreenName}");
                            continue;
                        }

                    }
                    bool followed = User.FollowUser(tweet.CreatedBy.Id);
                    LogDebug($"Autofollow {tweet.CreatedBy.Id} @{tweet.CreatedBy.ScreenName} - {followed} ");
                    IUser u = null;
                    if (!followed)
                    {
                        u = User.GetUserFromId(tweet.CreatedBy.Id);
                        if (u != null && !u.Following)
                        {
                            LogDebug($"Autofollow Requeueing {tweet.CreatedBy.Id} @{tweet.CreatedBy.ScreenName} - {followed} ");
                            FollowQueue.Enqueue(tweet);
                        }
                        else
                        {
                            if (u != null)
                                followed = u.Following;
                        }
                    }
                    if (followed)
                    {
                        u = u ?? User.GetUserFromId(tweet.CreatedBy.Id);
                        using (var repo = new TDbContext())
                        {
                            var existing = repo.Users.FirstOrDefault(x => x.Id == u.Id);
                            if (existing == null)
                            {
                                var user = u.ToDbUser();
                                repo.Users.Add(user);

                            }
                            else
                            {
                                existing.Following = true;
                                existing.FollowedDate = DateTime.Now;
                                repo.Users.Attach(existing);
                                repo.Entry(existing).State = System.Data.Entity.EntityState.Modified;
                            }
                            repo.SaveChanges();
                        }
                    }
                    //  1440/ 400 per day = 3.6 = *60 = 216‬ seconds 
                    var jsonUpdated = FollowQueue.ToList().ToJson();
                    System.IO.File.WriteAllText("AFQueue.Json", jsonUpdated);
                    if (followed && FollowQueue.Count > 0) System.Threading.Thread.Sleep(216 * 1000);
                }
                if (FollowQueue.Count > 0) LogDebug($"FollowQueue.Count = {FollowQueue.Count} ");
            }
            var json = FollowQueue.ToList().ToJson();
            System.IO.File.WriteAllText("AFQueue.Json", json);
            processingFollows = false;
            LogDebug($"Followed {followCount} in {sw.Elapsed}");

        }

        private void ProcessEnsures()
        {

            var sw = Stopwatch.StartNew();
            int ensureCount = EnsureQueue.Count;
            LogDebug($"Ensuring {ensureCount} ");
            while (EnsureQueue.Count > 0)
            {
                //LogDebug($"Ensuring {ensureCount} in {sw.Elapsed}");
                if (EnsureQueue.TryDequeue(out ITweet result))
                {
                    AssureMyUserTweet(result);
                }
            }
            LogDebug($"Ensured {ensureCount} in {sw.Elapsed}");
        }

        private void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now}: {message}");
        }
        private void LogDebug(string format, params object[] args)
        {
            LogDebug(string.Format(format, args));
        }

        private void ProcessTweet(ITweet tweet)
        {

            var metric = new Metric(tweet);
            metrics.Enqueue(metric);


            bool isInteraction = tweet.CreatedBy.Id == currentUser.Id
                || tweet.QuotedTweet != null && (long)tweet.QuotedTweet.CreatedBy.Id == currentUser.Id
                || tweet.RetweetedTweet != null && (long)tweet.RetweetedTweet.CreatedBy.Id == currentUser.Id
                || tweet.CurrentUserRetweetIdentifier != null && tweet.CurrentUserRetweetIdentifier.Id == currentUser.Id
                || (bool)tweet.UserMentions?.Any(x => (long)x.Id == currentUser.Id)
                || tweet.InReplyToUserId != null && (long)tweet.InReplyToUserId == currentUser.Id;
            if (isInteraction)
            {
                LogDebug($"Interaction {tweet.Id} @{tweet.CreatedBy.ScreenName} {tweet.Url}");
            }
            if (tweet.QuotedTweet != null && (tweet.CreatedBy.Id != currentUser.Id && tweet.QuotedTweet.CreatedBy.Id == currentUser.Id))
            {
                LogDebug($"Quoted Me {tweet.Id} @{tweet.CreatedBy.ScreenName} {tweet.Url}");
                EnsureQueue.Enqueue(tweet);
                if (!tweet.CreatedBy.Following)
                {
                    FollowQueue.Enqueue(tweet);
                }
                else
                {
                    LogDebug($"Autofollow {tweet.CreatedBy.Id} @{tweet.CreatedBy.ScreenName} - Already following ");
                }
            }
            else if (tweet.RetweetedTweet != null && (tweet.CreatedBy.Id == currentUser.Id || tweet.RetweetedTweet.CreatedBy.Id == currentUser.Id))
            {
                LogDebug($"Retweeted Me {tweet.Id} @{tweet.CreatedBy.ScreenName} {tweet.Url}");
                EnsureQueue.Enqueue(tweet);
                if (tweet.CreatedBy.Id != currentUser.Id)
                {
                    if (!tweet.CreatedBy.Following)
                    {
                        FollowQueue.Enqueue(tweet);
                    }
                    else
                    {
                        LogDebug($"Autofollow {tweet.CreatedBy.Id} @{tweet.CreatedBy.ScreenName} - Already following ");
                    }
                }
                else
                {
                    string bp = "This shouldn't happen";
                }
                LogDebug($"FollowQueue.Count = {FollowQueue.Count}");
            }
            else if (tweet.InReplyToUserId != null && tweet.InReplyToUserId == currentUser.Id)
            {
                LogDebug($"Replied to Me {tweet.Id} @{tweet.CreatedBy.ScreenName} {tweet.Url}");
                if (!myTweetIds.Contains(tweet.Id))
                {
                    var t = Tweet.GetTweet((long)tweet.InReplyToStatusId);
                    EnsureQueue.Enqueue(t);
                }

            }
            else if (tweet.CreatedBy.Id == currentUser.Id)
            {
                LogDebug($"My Tweet {tweet.Id} @{tweet.CreatedBy.ScreenName} {tweet.Url}");
                EnsureQueue.Enqueue(tweet);
            }

        }

        private void Stream_MatchingTweetReceived(object sender, MatchedTweetReceivedEventArgs e)
        {
            ProcessTweet(e.Tweet);
        }

        private void AssureMyUserTweet(ITweet tweet)
        {
            if (myTweetIds.Contains(tweet.Id))
                return;
            myTweetIds.Add(tweet.Id);
            bool saved = false;
            int timeout = 1000;
            int maxTimeout = 30000;
            DbLockResult dbLock = null;
            while (!saved)
            {
                try
                {
                    using (var repo = new TDbContext())
                    {
                        dbLock = dbLock ?? DbLockHelper.GetDbLock((SqlConnection)repo.Database.Connection);
                        var metric = repo.Metrics.FirstOrDefault(x => x.TweetId == (long)tweet.Id);
                        if (metric == null)
                        {
                            metric = new Metric(tweet);
                            metrics.Enqueue(metric);
                        }
                        DbLockHelper.ReleaseLock((SqlConnection)repo.Database.Connection, dbLock);
                    }
                    saved = true;
                }
                catch (Exception ex)
                {
                    LogDebug($"Error Assuring {ex.Message}");
                    System.Threading.Thread.Sleep(timeout);
                    timeout = Math.Min(timeout << 1, maxTimeout);
                }
            }
            dbLock = null;
        }

        private void Stream_NonMatchingTweetReceived(object sender, Tweetinvi.Events.TweetEventArgs e)
        {
            ProcessTweet(e.Tweet);
        }



        private void Stream_LimitReached(object sender, Tweetinvi.Events.LimitReachedEventArgs e)
        {
            LogDebug($"{DateTime.Now}: {nameof(Stream_LimitReached)}");
        }

        private void Stream_DisconnectMessageReceived(object sender, Tweetinvi.Events.DisconnectedEventArgs e)
        {
            saving = false;
            LogDebug($"{nameof(Stream_DisconnectMessageReceived)}");
        }

        private void Stream_TweetDeleted(object sender, Tweetinvi.Events.TweetDeletedEventArgs e)
        {
            var metric = new Metric(e.TweetId, e.UserId, DateTime.Now, true);
            metrics.Enqueue(metric);
        }

        private void Stream_StreamStopped(object sender, Tweetinvi.Events.StreamExceptionEventArgs e)
        {
            LogDebug($"{nameof(Stream_StreamStopped)}");
            if (!canceled)
            {
                Start();
            }
        }

        private void Stream_StreamStarted(object sender, EventArgs e)
        {
            LogDebug($"{nameof(Stream_StreamStarted)}");
            LastStartDate = DateTime.Now;
        }
    }
}

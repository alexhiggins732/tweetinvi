using Dapper;
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
using Tweetinvi.Models;

namespace ConsoleApp1
{
    class Program
    {
        static Program() { Examplinvi.Creds.Helper.SetCreds(); }
        static void Main(string[] args)
        {
            Processor.Start();

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
    public class Processor : IDisposable
    {
        public static void Start()
        {
            var processor = new Processor();
            processor.Run();
        }
        private Timer queryTimer;
        private Timer postTimer;
        private Timer cleanupTimer;
        string connString = "server=.;initial catalog=tdb;trusted_connection=true";
        ConcurrentQueue<Eng> queue = null;
        private IAuthenticatedUser current;

        public Processor()
        {
            current = User.GetAuthenticatedUser();
        }

        public void Dispose()
        {
            if (queryTimer != null)
            {
                queryTimer.Stop();
                queryTimer.Elapsed -= QueryTimer_Elapsed;
                queryTimer = null;
            }

            if (postTimer != null)
            {
                postTimer.Stop();
                postTimer.Elapsed -= PostTimer_Elapsed;
                postTimer = null;
            }

            if (cleanupTimer != null)
            {
                cleanupTimer.Stop();
                cleanupTimer.Elapsed -= CleanupTimer_Elapsed;
                cleanupTimer = null;
            }

        }

        bool canceled = false;
        const int seconds = 1000;
        const int minute = 60 * seconds;
        List<long> processed = null;
        public void Run()
        {
            queue = queue ?? new ConcurrentQueue<Eng>();
            processed = processed ?? new List<long>();
            if (queryTimer == null)
            {
                queryTimer = queryTimer ?? new Timer();
                queryTimer.Interval = 10 * minute;
                queryTimer.AutoReset = true;
                queryTimer.Elapsed += QueryTimer_Elapsed;
            }

            if (postTimer == null)
            {
                postTimer = postTimer ?? new Timer();
                postTimer.Interval = 1 * minute;
                postTimer.AutoReset = true;
                postTimer.Elapsed += PostTimer_Elapsed;
            }


            if (cleanupTimer == null)
            {
                cleanupTimer = cleanupTimer ?? new Timer();
                cleanupTimer.Interval = 3 * minute;
                cleanupTimer.AutoReset = true;
                cleanupTimer.Elapsed += CleanupTimer_Elapsed;
            }

            Cleanup();
            QuerySingle();
            ProcessSingle();

            queryTimer.Start();
            postTimer.Start();
            cleanupTimer.Start();


            while (!canceled)
            {
                System.Threading.Thread.Sleep(1000);

                while (processed.Count > 50)
                    processed.RemoveAt(0);
                
                if (bool.Parse(bool.FalseString))
                {
                    var userIds = User.GetUsersFromScreenNames(new[] { "comey" });
                    var inList = string.Join("\r\n", userIds.Select(x => $"{x.Id}, -- @{x.ScreenName}: {x.Name}"));
                    var users = User.GetUsersFromIds(new[] { 910492003359760384, 1179485989854744576 });
                    QuerySingle();
                    ProcessSingle();
                }

            }
        }

        private void CleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Cleanup();
        }
        bool cleaningUp = false;
        private void Cleanup()
        {
            LogDebug($"{nameof(CleanupTimer_Elapsed)}: {nameof(cleaningUp)} = {cleaningUp}");
            if (cleaningUp)
                return;
            cleaningUp = true;
            int commandTimeout = 960;
            int cleaned = 0;

            bool hasMore = true;
            DbLockResult dbLock = null;
            int sleepTimeout = 100;
            int maxSleepTimeout = 10000;
            while (hasMore)
            {

                LogDebug($"Executing cleanup with {nameof(commandTimeout)} of {commandTimeout} ");
                try
                {
                    var sw = Stopwatch.StartNew();
                    using (var conn = new SqlConnection(connString))
                    {
                        dbLock = dbLock ?? DbLockHelper.GetDbLock(conn);
                        cleaned = conn.QueryFirst<int>("CleanupExpired2", commandType: System.Data.CommandType.StoredProcedure, commandTimeout: commandTimeout);
                        DbLockHelper.ReleaseLock(conn, dbLock);
                    }
                    LogDebug($"Executed cleanup of {cleaned} in {sw.Elapsed}");
                    hasMore = cleaned > 100000;
                }
                catch (Exception ex)
                {
                    sleepTimeout <<= 1;
                    if (sleepTimeout > maxSleepTimeout) sleepTimeout = maxSleepTimeout;

                    LogDebug($"Failed executing cleanup: {ex.Message}");
                    System.Threading.Thread.Sleep(sleepTimeout);
                }
            }
            dbLock = null;
            cleaningUp = false;
            try
            {
                LogDebug($"Executing updatestats with timeoutout of {commandTimeout} ");
                var sw = Stopwatch.StartNew();
                using (var conn = new SqlConnection(connString))
                {
                    //dbLock = dbLock ?? DbLockHelper.GetDbLock(conn);
                    conn.QueryFirstOrDefault<int?>("sp_updatestats", commandType: System.Data.CommandType.StoredProcedure, commandTimeout: commandTimeout);
                    //DbLockHelper.ReleaseLock(conn, dbLock);
                }
                LogDebug($"Updated Stats in {sw.Elapsed}");
            }
            catch (Exception ex)
            {
                LogDebug($"Failed executing cleanup: {ex.Message}");
            }


        }

        private void PostTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProcessSingle();
            // queryTimer.Interval = postTimer.Interval = 10 * minute;
        }

        private void ProcessSingle()
        {
            bool sent = false;
            var queueCount = queue.Count;
            LogDebug($"Processing Queue: {queueCount} item(s)");
            var sw = Stopwatch.StartNew();
            int processedCount = 0;
            while (queue.Count > 0 && !sent)
            {
                if (queue.Count > 0)
                {
                    if (queue.TryDequeue(out Eng result))
                    {
                        processedCount++;
                        var t = Tweet.GetTweet(result.TweetId);

                        if (t != null)
                        {
                            if (processed.Contains(t.Id))
                            {
                                LogDebug($"Duplicate {t.Id}: @{t.CreatedBy.ScreenName} - {t.Text}");
                            }
                            else
                            {
                                processed.Add(t.Id);
                                LogDebug($"Published {t.Id}: @{t.CreatedBy.ScreenName} - {t.Text}");
                                t.PublishRetweet();
                                sent = true;
                            }
                        }
                    }
                }
            }

            LogDebug($"Processed {processedCount} in {sw.Elapsed}");

        }

        private void LogDebug(string format, params object[] args)
        {
            LogDebug(string.Format(format, args));
        }
        private void LogDebug(string message)
        {
            var line = $"{DateTime.Now} - {message}";
            System.Diagnostics.Debug.WriteLine(line);
            Console.WriteLine(line);
        }

        bool querying = false;
        private void QuerySingle()
        {
            LogDebug($"Querying single: {querying}");
            if (querying)
                return;
            querying = true;
            var sw = Stopwatch.StartNew();

            Eng top = null;
            var timeout = 50;
            var maxTimeout = 10000;
            bool queried = false;
            var commandTimeout = 240;
            LogDebug($"QuerySingle()");
            //DbLockResult dbLock = null;
            int retryCount = 0;
            while (!queried)
            {
                try
                {
                    using (var conn = new SqlConnection(connString))
                    {
                        //dbLock = dbLock ?? DbLockHelper.GetDbLock(conn);
                        var query = "select top 1 * from VwTopEng with (nolock) where engid is null and Engagements > 40 ";
                        if (processed.Count > 0)
                        {
                            var rmQuery = $"select tweetId from VwTopEng where engid is not null and TweetId in ({string.Join(", ", processed)})";
                            List<long> toRemove = conn.Query<long>(rmQuery, commandTimeout: commandTimeout)
                                .ToList();
                            processed = processed.Except(toRemove).ToList();
                            if (processed.Count > 0)
                            {
                                query += $" and TweetId not in ({string.Join(",", processed)})";
                            }
                        }
                        LogDebug($"Executing {nameof(QuerySingle)}");
                        sw.Restart();
                        top = conn.QuerySingleOrDefault<Eng>(query + " ", commandTimeout: 480);
                        LogDebug($"Executed{nameof(QuerySingle)} in {sw.Elapsed}");
                        queried = true;
                        //DbLockHelper.ReleaseLock(conn, dbLock);
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= 3)
                    {

                        using (var conn = new SqlConnection(connString))
                        {
                            {
                                conn.Execute("sp_updatestats", commandType: CommandType.StoredProcedure, commandTimeout: 960);
                                conn.Execute("CleanupExpired2", commandType: CommandType.StoredProcedure, commandTimeout: 960);
                                conn.Execute("sp_updatestats", commandType: CommandType.StoredProcedure, commandTimeout: 960);
                                conn.Close();
                            }
                        }
                    }
                    LogDebug($"Error {nameof(QuerySingle)}: {ex.Message}");
                    System.Threading.Thread.Sleep(timeout);
                    timeout = Math.Max(maxTimeout, timeout << 1);
                }
            }
            //dbLock = null;
            if (top != null)
            {
                queue.Enqueue(top);
                LogDebug($"Queued: {top.TweetId}");
            }
            else
            {
                LogDebug($"No results to queue");
            }
            LogDebug($"QuerySingle executed in {sw.Elapsed}");
            querying = false;
        }
        private void QueryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            QuerySingle();

        }

    }
    public class Eng
    {
        public int? EngId { get; set; }
        public long TweetId { get; set; }
        public long UserId { get; set; }
        public int Engagements { get; set; }
        public int RtCount { get; set; }
        public int ReplyCount { get; set; }
        public int QuoteCount { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Elaspse { get; set; }
        public decimal EPM { get; set; }
    }
}

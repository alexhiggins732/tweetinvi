using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Examplinvi.DbFx.Models;
using Examplinvi.Creds;
using Examplinvi.DbFx;
using System.IO;

namespace TwitterDeleter
{

    public class ArchiveDeleter
    {
        private Archiver archiver = new Archiver();
        public void Run()
        {
            var parent = @"C:\Users\Alexander\Downloads\twitter-2021-01-09-a57f6ceb0bc0470468ab09e32051425b3f802716f59a4a9b687f26cbc2e681d9\data";
            var di = new DirectoryInfo(parent);
            var files = di.GetFiles("Tweet*.js").OrderBy(x => x.Name).ToList();
            foreach (var file in files)
            {
                DeleteTweets(file);

            }
        }

        private int count;
        private void DeleteTweets(FileInfo file)
        {
            count = 0;
            using (var sr = new StreamReader(file.FullName))
            {
                var buffer = new StringBuilder();
                while (sr.Read() != (int)'[')
                {

                }
                var line = "";
                string tweetJson = "";

                var dtoes = new List<Tweetinvi.Logic.DTO.TweetDTO>();
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    if (line != "}, {")
                    {
                        buffer.AppendLine(line);
                    }
                    else
                    {
                        buffer.AppendLine("}");
                        tweetJson = buffer.ToString();
                        dtoes.Add(GetTweet(tweetJson));
                        buffer.Clear();
                        buffer.AppendLine("{");
                    }
                    if (dtoes.Count == 100)
                    {
                        DeleteTweets(dtoes);
                        dtoes.Clear();
                    }
                }

                line = buffer.ToString().Trim();
                line = line.Substring(0, line.Length - 1);
                Console.Title = $"Deleteing Tweet {++count}";
                DeleteTweet(line);

            }
        }

        private void DeleteTweets(List<Tweetinvi.Logic.DTO.TweetDTO> dtoes)
        {
            var dict = dtoes.ToDictionary(x => x.Id, x => x);
            var tweetIds = dtoes.Select(x => x.Id).ToArray();
            var tweets = Tweet.GetTweets(tweetIds).ToDictionary(x => x.Id, x => x);
            foreach (var dto in dtoes)
            {
                if (tweets.ContainsKey(dto.Id))
                {
                    Console.Title = $"Deleteing Tweet {++count}";
                    var tweet = tweets[dto.Id];
                    Console.WriteLine("-".PadLeft(Console.BufferWidth, '-'));
                    Console.WriteLine($"[{DateTime.Now}]: Deleting {tweet.Id} [{tweet.CreatedAt}]: {tweet.Text}");
                    archiver.Archive(tweet);
                    System.Threading.Thread.Sleep(1000);
                } else
                {
                    Console.Title = $"Deleteing Tweet {++count}";
                    Console.WriteLine("-".PadLeft(Console.BufferWidth, '-'));
                    Console.WriteLine($"[{DateTime.Now}]: Already Deleted {dto.Id} [{dto.CreatedAt}]: {dto.FullText}");
                }
            }

        }

        private Tweetinvi.Logic.DTO.TweetDTO GetTweet(string tweetJson)
        {
            var trimmed = tweetJson.Trim().Substring(" {\r\n  \"tweet\" :".Length);
            trimmed = trimmed.Substring(0, trimmed.Length - 2);
            var it = JsonSerializer.ConvertJsonTo<Tweetinvi.Logic.DTO.TweetDTO>(trimmed);
            return it;
        }



        private bool DeleteTweet(string tweetJson)
        {

            var trimmed = tweetJson.Trim().Substring(" {\r\n  \"tweet\" :".Length);
            trimmed = trimmed.Substring(0, trimmed.Length - 2);
            var it = JsonSerializer.ConvertJsonTo<Tweetinvi.Logic.DTO.TweetDTO>(trimmed);
            var tweet = Tweet.GetTweet(it.Id);
            if (tweet == null)
            {
                Console.WriteLine("-".PadLeft(Console.BufferWidth, '-'));
                Console.WriteLine($"[{DateTime.Now}]: Already Deleted {it.Id} [{it.CreatedAt}]: {it.FullText}");
                return false;
            }
            Console.WriteLine("-".PadLeft(Console.BufferWidth, '-'));
            Console.WriteLine($"[{DateTime.Now}]: Deleting {tweet.Id} [{tweet.CreatedAt}]: {tweet.Text}");
            archiver.Archive(tweet);
            return true;
        }
        private void DeleteTweetOl(string tweetJson)
        {
            var trimmed = tweetJson.Trim().Substring(" {\r\n  \"tweet\" :".Length);
            trimmed = trimmed.Substring(0, trimmed.Length - 2);
            var tw = JsonSerializer.ConvertJsonTo<Archived.ArchivedTweet>(tweetJson);

            var it = JsonSerializer.ConvertJsonTo<Tweetinvi.Logic.DTO.TweetDTO>(trimmed);
            var factory = TweetinviContainer.Resolve<Tweetinvi.Core.Factories.ITweetFactory>();
            var tweet = factory.GenerateTweetFromDTO(it);

            Console.WriteLine("-".PadLeft(Console.BufferWidth, '-'));
            Console.WriteLine($"[{DateTime.Now}]: Deleting {tweet.Id}: {tweet.Text}");
            using (var ctx = new TDbContext())
            {
                var dbTweet = ctx.Tweets.Where(x => x.Id == it.Id).FirstOrDefault();
                if (dbTweet == null)
                {
                    dbTweet.Deleted = true;
                    ctx.Tweets.Add(dbTweet);
                    ctx.SaveChanges();

                }
                else
                {
                    ctx.Entry(dbTweet).State = System.Data.Entity.EntityState.Deleted;
                    ctx.SaveChanges();
                }
            }
        }

    }
    class Program
    {
        static bool credsAreSet = false;
        static void SetCreds()
        {
            if (credsAreSet) return;
            credsAreSet = true;
            //Auth.SetUserCredentials("CONSUMER_KEY", "CONSUMER_SECRET", "ACCESS_TOKEN", "ACCESS_TOKEN_SECRET");
            Auth.SetUserCredentials(Settings.CONSUMER_KEY, Settings.CONSUMER_SECRET,
                Settings.ACCESS_TOKEN, Settings.ACCESS_TOKEN_SECRET);
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
            // Publish the Tweet "Hello World" on your Timeline
            //Tweet.PublishTweet("Hello World!");

        }
        static Program() => SetCreds();
        static void Main(string[] args)
        {
            var m = User.GetAuthenticatedUser();
            var deleter = new ArchiveDeleter();
            deleter.Run();
            var slug = $"{m.Id}-{m.ScreenName}";
            Delete();
            Archive();

        }

        private static void Delete()
        {
            var user = User.GetAuthenticatedUser();
            var deleted = 0;
            using (var ctx = new TDbContext())
            {
                var dbTweets = ctx.Tweets.Where(x => x.UserId == user.Id).OrderBy(x => x.CreatedAt).ToList();
                foreach (var item in dbTweets)
                {
                    var txt = item.Text;
                    Console.Title = $"Deleting Tweet {++deleted}";
                    Console.WriteLine($"-".PadLeft(Console.BufferWidth, '-'));
                    Console.WriteLine($"{item.CreatedAt} {txt}");
                    System.Threading.Thread.Sleep(15000);
                    Tweet.DestroyTweet(item.Id);
                    ctx.Entry(item).State = System.Data.Entity.EntityState.Deleted;
                    ctx.SaveChanges();
                }
            }
        }

        static void Archive()
        {

            var archiver = new Archiver();
            Console.WriteLine($"Running {nameof(Archiver)}.{nameof(Archiver.Run)}");
            archiver.Run();
            Console.WriteLine($"Finished {nameof(Archiver)}.{nameof(Archiver.Run)}");

        }
    }

    public class Archiver
    {
        public Archiver()
        {
        }

        public void Run()
        {
            var user = User.GetAuthenticatedUser();
            var myName = user.ScreenName;
            var tlParameters = new Tweetinvi.Parameters.UserTimelineParameters
            {
                ExcludeReplies = false,
                IncludeContributorDetails = false,
                IncludeEntities = true,
                IncludeRTS = true,
                MaxId = -1,
                MaximumNumberOfTweetsToRetrieve = 200,
                SinceId = -1,
                TrimUser = false
            };
            var t = Timeline.GetUserTimeline(user, tlParameters).ToList();
            long sinceId = long.MaxValue;
            string txt = "";
            int archived = 0;
            while (t.Count > 0)
            {

                foreach (var item in t)
                {
                    if (item.CreatedBy.ScreenName == myName)
                    {
                        //Archive(item);

                    }
                    txt = item.Text;
                    sinceId = Math.Min(item.Id, sinceId);
                    Console.Title = $"Archived Tweet {++archived}";
                    Console.WriteLine($"-".PadLeft(Console.BufferWidth, '-'));
                    Console.WriteLine($"{item.CreatedAt} {txt}");
                }
                tlParameters.MaxId = sinceId;
                t = Timeline.GetUserTimeline(user, tlParameters).Skip(1).ToList();
            }


        }



        public void Archive(ITweet item)
        {
            //throw new NotImplementedException();
            //1346653312561590272
            var tweet = item.ToDbTweet();
            var m = tweet.Media.ToList();
            tweet.Media.Clear();
            var len = tweet.Text.Length;
            try
            {

                using (var ctx = new TDbContext())
                {
                    var dbTweet = ctx.Tweets.Where(x => x.Id == item.Id).FirstOrDefault();
                    if (dbTweet != null)
                    {
                        ctx.Tweets.Attach(dbTweet);
                        ctx.Entry(dbTweet).State = System.Data.Entity.EntityState.Deleted;
                        ctx.SaveChanges();
                        Tweet.DestroyTweet(item);
                        return; //if (dbTweet.Deleted)

                    }
                    DownloadMedia(item);
                    foreach (var media in m.ToList())
                    {
                        var dbMedia = ctx.Media.SingleOrDefault(x => x.Id == media.Id);

                        if (dbMedia != null)
                        {
                            tweet.Media.Remove(media);
                            tweet.Media.Add(dbMedia);
                        }
                        else
                        {
                            ctx.Media.Add(media);

                        }
                        //ctx.SaveChanges();
                    }

                    ctx.Tweets.Add(tweet);
                    ctx.SaveChanges();
                    Tweet.DestroyTweet(item);
                }


            }
            catch (Exception ex)
            {
                string bp = ex.Message;
            }
        }

        private void DownloadMedia(ITweet t)
        {
            if (!t.Media.Any()) return;
            if (t.Media.Any(x => x.MediaType == "video"))
            {
                var m = t.Media.First(x => x.MediaType == "video");
                var variant = m.VideoDetails.Variants
                    .First(x => x.ContentType == "video/mp4");
                var fileName = Path.GetFileName(variant.URL.Split('?')[0]);
                var mediaDirectory = Directory.CreateDirectory("videos");
                var dest = Path.Combine(mediaDirectory.FullName, fileName);
                if (File.Exists(dest))
                {
                    return;
                }
                var id = t.Id;

                var data = TwitterAccessor.DownloadBinary(variant.URL);



                File.WriteAllBytes(dest, data);
                Console.WriteLine("Saved to {0}", fileName);

            }
            else
            {
                foreach (var m in t.Media)
                {
                    var fileName = Path.GetFileName(m.MediaURL); //Path.GetFileName(m.URL.Split('?')[0]);
                    var mediaDirectory = Directory.CreateDirectory("Images");
                    var dest = Path.Combine(mediaDirectory.FullName, fileName);
                    if (File.Exists(dest))
                    {
                        return;
                    }
                    var data = TwitterAccessor.DownloadBinary(m.MediaURL);
                    File.WriteAllBytes(dest, data);
                    Console.WriteLine("Saved image to {0}", fileName);
                }
            }

        }
    }
}

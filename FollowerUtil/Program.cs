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

namespace FollowerUtil
{
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

        static void log(string message)
        {
            Console.WriteLine(message);
            File.AppendAllLines("Suspended.txt", new[] { message });
        }
        static void Main(string[] args)
        {
            File.Delete("Suspended.txt");
            var archivedFollowers = LoadArchiveFollowers();
            Console.WriteLine($"Looking up {archivedFollowers.Count} followers");

            int start = 0;

            List<Follower> missingFollowers = new List<Follower>();
            while (start < archivedFollowers.Count)
            {
                var batch = archivedFollowers.Skip(start).Take(100);
                var ids = batch.Select(x => x.accountId);
                var users = User.GetUsersFromIds(ids);
                var userIds = users.Select(x => x.Id).ToList();
                var missingUsersIds = ids.Except(userIds).ToList();
                var existing = userIds.Except(missingUsersIds);

                var batchMissing = batch.Where(x => missingUsersIds.Contains(x.accountId)).ToList();
                foreach (Follower missing in batchMissing)
                {
                    missingFollowers.Add(missing);
                    var userLink = $"https://twitter.com/i/user/{missing.accountId}";
                    missing.userLink = userLink;
                    log($"[{missingFollowers.Count}] Suspended {missing.accountId}: {missing.userLink}");
                    missing.status = "Suspended";
                }
                System.Threading.Thread.Sleep(1000);
                start += batch.Count();
                Console.Title = $"Checked {start} of {archivedFollowers.Count} - {missingFollowers.Count} users suspended";
            }
            Console.Title = $"Checked {start} of {archivedFollowers.Count} - {missingFollowers.Count} users suspended";

            var dbUsers = new Dictionary<long, DbUser>();
            using (var ctx = new TDbContext())
            {
                var db = ctx.Users.Select(x => x).ToList();
                foreach (var u in db)
                {
                    dbUsers.Add(u.Id, u);
                }
            }
            int foundDbCount = 0;
            foreach(var u in missingFollowers)
            {
                if (dbUsers.ContainsKey(u.accountId))
                {
                    var dbUser = dbUsers[u.accountId];
                    var userScreenName = dbUser.ScreenName;
                    foundDbCount++;
                    u.userLink = $"https://twitter.com/{userScreenName}";
                    log($"Banned Db User: {dbUser.Id} { u.userLink } - {dbUser.Description}");
                }
            }


            log($"Found {foundDbCount} of {missingFollowers.Count} users in Database");

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(missingFollowers, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("suspended.json", json);
        }
        public static List<Follower> LoadArchiveFollowers()
        {
            var result = new List<Follower>();
            var json = File.ReadAllText(@"C:\Users\Alexander\Downloads\twitter-2021-01-09-a57f6ceb0bc0470468ab09e32051425b3f802716f59a4a9b687f26cbc2e681d9\data\follower.js");
            json = json.Substring("window.YTD.follower.part0 = ".Length);
            var coll = JsonSerializer.ConvertJsonTo<FollowerRoot[]>(json);
            //result.AddRange(coll.FollowerRoot.Select(x => x.follower));
            result.AddRange(coll.Select(x => x.follower));
            return result;
        }
    }

    public class FollowerCollection
    {
        public FollowerRoot[] FollowerRoot { get; set; }
    }

    public class FollowerRoot
    {
        public Follower follower { get; set; }
    }

    public class Follower
    {
        public long accountId { get; set; }
        public string userLink { get; set; }
        public string status { get; set; }
    }

}

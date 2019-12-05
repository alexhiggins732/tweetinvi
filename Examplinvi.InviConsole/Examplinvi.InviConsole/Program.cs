using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using System.IO;
using Tweetinvi.Models.Entities;
using Tweetinvi.Models.DTO.QueryDTO;
using Tweetinvi.Credentials.QueryDTO;
using Examplinvi.DbFx.Models;
using System.Data.Entity.Design;
using System.Globalization;
using System.Data.Entity.Design.PluralizationServices;


namespace Examplinvi.InviConsole
{
    public enum RefreshMode
    {
        None = 0,
        RefreshFromFileOrApi = 1,
        RefreshFromApi = 2,
    }
    public class RelHelper
    {
        public const RefreshMode DefaultRefreshMode = RefreshMode.None;
        public RelHelper()
        {
            Auth.SetUserCredentials(Creds.Settings.CONSUMER_KEY, Creds.Settings.CONSUMER_SECRET,
                Creds.Settings.ACCESS_TOKEN, Creds.Settings.ACCESS_TOKEN_SECRET);
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
            CurrentUser = User.GetAuthenticatedUser();
        }

        public IAuthenticatedUser CurrentUser;
        List<IUser> friends;
        List<IUser> followers;

        public List<IUser> Friends => friends ?? GetFriends();
        public List<IUser> Followers => followers ?? GetFollowers();


        public List<IUser> GetFriends(RefreshMode refresh = DefaultRefreshMode)
        {
            Func<List<IUser>> refreshAction = null;
            Func<List<IUser>> refreshFromApi = () => { friends = CurrentUser.GetFriends(Int32.MaxValue).ToList(); SaveFriends(); return friends; };
            Func<List<IUser>> refreshFromFile = () => JsonSerializer.ConvertJsonTo<List<IUser>>(File.ReadAllText($"{nameof(friends)}.json"));
            Func<List<IUser>> refreshFromFileOrApi = File.Exists($"{nameof(friends)}.json") ? refreshFromFile : refreshFromApi;

            Func<List<IUser>> refreshFromMemory = () => friends;
            switch (refresh)
            {
                case RefreshMode.RefreshFromApi:
                    refreshAction = refreshFromApi;
                    break;
                case RefreshMode.RefreshFromFileOrApi:
                    refreshAction = refreshFromFileOrApi;
                    break;
                case RefreshMode.None:
                    refreshAction = friends is null ? refreshFromFileOrApi : refreshFromMemory;
                    break;

            }
            friends = refreshAction();
            return friends;
        }


        public List<IUser> GetFollowers(RefreshMode refresh = DefaultRefreshMode)
        {
            Func<List<IUser>> refreshAction = null;
            Func<List<IUser>> refreshFromApi = () => { followers = CurrentUser.GetFollowers(Int32.MaxValue).ToList(); SaveFollowers(); return followers; };
            Func<List<IUser>> refreshFromFile = () => JsonSerializer.ConvertJsonTo<List<IUser>>(File.ReadAllText($"{nameof(followers)}.json"));
            Func<List<IUser>> refreshFromFileOrApi = File.Exists($"{nameof(followers)}.json") ? refreshFromFile : refreshFromApi;

            Func<List<IUser>> refreshFromMemory = () => followers;
            switch (refresh)
            {
                case RefreshMode.RefreshFromApi:
                    refreshAction = refreshFromApi;
                    break;
                case RefreshMode.RefreshFromFileOrApi:
                    refreshAction = refreshFromFileOrApi;
                    break;
                case RefreshMode.None:
                    refreshAction = followers is null ? refreshFromFileOrApi : refreshFromMemory;
                    break;

            }
            followers = refreshAction();
            return followers;


        }

        public void SaveFriends()
        {
            var fileName = $"{nameof(friends)}.json";
            if (File.Exists(fileName))
            {
                var backupFileName = $"{nameof(friends)}_{DateTime.Now.ToString("yyyy_MM_dd-hh_mm_ss")}.json";
                File.Move(fileName, backupFileName);
            }
            File.WriteAllText(fileName, friends.ToJson());
        }

        public void SaveFollowers()
        {
            var fileName = $"{nameof(followers)}.json";
            if (File.Exists(fileName))
            {
                var backupFileName = $"{nameof(followers)}_{DateTime.Now.ToString("yyyy_MM_dd-hh_mm_ss")}.json";
                File.Move(fileName, backupFileName);
            }

            File.WriteAllText(fileName, followers.ToJson());
        }

        public List<long> GetFriendIds(RefreshMode refresh = DefaultRefreshMode)
        {
            return GetFriends(refresh).Select(x => x.Id).ToList();
        }

        public List<long> GetFollowerIds(RefreshMode refresh = DefaultRefreshMode)
        {
            return GetFollowers(refresh).Select(x => x.Id).ToList();
        }

        public List<long> GetFriendIdsNotFollowing(RefreshMode refresh = DefaultRefreshMode)
        {
            var friendsIds = GetFriendIds(refresh);
            var followerIds = GetFollowerIds(refresh);
            return friendsIds.Where(id => !followerIds.Contains(id)).ToList();
        }

        public List<IUser> GetFriendsNotFollowing(RefreshMode refresh = DefaultRefreshMode)
        {
            var friendIdsNotFollowingIds = GetFriendIdsNotFollowing(refresh);
            var friends = GetFriends(refresh);
            var result = friends.Where(friend => friendIdsNotFollowingIds.Contains(friend.Id)).ToList();
            var orderedResult = result.OrderBy(x => x.FollowersCount).ToList();
            return orderedResult;
        }



        public List<long> GetFollwersIdsWithoutFriends(RefreshMode refresh = DefaultRefreshMode)
        {
            var friendsIds = GetFriendIds(refresh);
            var followerIds = GetFollowerIds(refresh);
            return followerIds.Where(id => !friendsIds.Contains(id)).ToList();
        }

        internal void Unfollow(IUser user)
        {
            CurrentUser.UnFollowUser(user);
            var removed = friends.Remove(user);
            System.Diagnostics.Debug.Assert(removed);
        }

        internal void RefreshFromApi()
        {
            GetFriends(RefreshMode.RefreshFromApi);
            GetFollowers(RefreshMode.RefreshFromApi);
        }
    }

    public class JsonData
    {
        public static T LoadFileOrDefault<T>(string fileName)
            where T : class, new()
        {
            if (File.Exists(fileName))
            {
                return JsonSerializer.ConvertJsonTo<T>(File.ReadAllText(fileName));
            }
            return new T();
        }
    }
    class Program
    {

        static Program()
        {
            SetCreds();
        }
        static void Main(string[] args)
        {
            UpdateFollowing();
            if (bool.Parse(bool.TrueString))
                return;
            RelFixer.run();
            GetDTOS();
          
            UpdateFollowing();
            TestDb();
            //WhiteListFollowed();
            //special();

            if (args.Length > 0)
            {
                Uri uriResult = null;
                if (Uri.TryCreate(args[0], UriKind.Absolute, out uriResult))
                {
                    {
                        DownloadVideo(uriResult);
                        return;
                    }
                }
                else
                {
                    switch (args[0].ToLower())
                    {
                        case "clone":
                            {
                                if (Uri.TryCreate(args[1], UriKind.Absolute, out uriResult))
                                {
                                    {
                                        CloneAndPubWithMedia(uriResult.ToString());
                                        return;
                                    }
                                }
                            }
                            break;
                        case "download":
                            {
                                if (Uri.TryCreate(args[1], UriKind.Absolute, out uriResult))
                                {
                                    {
                                        DownloadVideo(uriResult);
                                        return;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            // monitorLikes();
            //TestDb();

            // ProcessTL(null);
            //AutoUnfollow();


        }
        static long GetId(string url) => GetId(new Uri(url));

        static long GetId(Uri uri)
        {
            var path = uri.AbsolutePath;
            var segments = path.Split('/');
            long id = 0;
            for (var i = 0; !long.TryParse(segments[i], out id) && i < segments.Length; i++)
            {
            }
            return id;
        }


        public static IMedia UploadImage(string filepath)
        {
            var imageBinary = File.ReadAllBytes(filepath);
            var media = Upload.UploadBinary(imageBinary);
            return media;
        }

        public static IMedia UploadVideo(string filepath)
        {
            var videoBinary = File.ReadAllBytes(filepath);

            var media = Upload.UploadVideo(videoBinary, new Tweetinvi.Core.Public.Parameters.UploadVideoOptionalParameters()
            {
                UploadStateChanged = uploadChangeEventArgs =>
                {
                    Console.WriteLine(uploadChangeEventArgs.Percentage);
                }
            });

            return media;
        }
        static void CloneAndPubWithMedia(string url, string text = null)
        {

            var t = Tweet.GetTweet(GetId(url));

            var mediaPath = DownloadMedia(t);
            var video = UploadVideo(mediaPath);

            if (text == null)
            {
                text = $"@{t.CreatedBy.ScreenName}: {t.Text} \r\n\r\n via @{User.GetAuthenticatedUser().ScreenName}";
            }
            var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
            {
                Medias = new List<IMedia>() { video }
            };
            Tweet.PublishTweet(text ?? t.Text, tParams);
        }

        static void DownloadVideo(Uri uri)
        {
            SetCreds();
            var id = GetId(uri);
            var t = Tweet.GetTweet(id);
            DownloadMedia(t);
            //var m = t.Media.First(x => ; x.MediaType == "video");
        }
        static void DownloadVideo(string url) => DownloadVideo(new Uri(url));

        static void TweetWithVideo(string text, string mediaPath)
        {
            SetCreds();
            var video = UploadVideo(mediaPath);
            var tParams = new Tweetinvi.Parameters.PublishTweetOptionalParameters()
            {
                Medias = new List<IMedia>() { video }

            };
            Tweet.PublishTweet(text, tParams);
        }

        static void WhiteListFollowed()
        {
            using (var ctx = new DbFx.TDbContext())
            {
                var users = ctx.Users.Where(x => x.Following && !x.WhiteListed).ToList();
                //users.ForEach(user => { user.WhiteListed = true; });
                foreach (var user in users)
                {
                    user.WhiteListed = true;
                    ctx.Entry<DbUser>(user).State = System.Data.Entity.EntityState.Modified;
                }
                var count = ctx.SaveChanges();
            }
        }

        static void GetDTOS()
        {
            CultureInfo ci = new CultureInfo("en-us");
            PluralizationService ps = PluralizationService.CreateService(ci);
            var type = typeof(Tweetinvi.Logic.DTO.UserDTO);
            var assem = type.Assembly;
            var DTOTypes = assem.GetExportedTypes().Where(x => x.Namespace == "Tweetinvi.Logic.DTO")
                .Where(x => x.GetProperty("Id") != null).ToList();
            //var DbSetStrings = DTOTypes.Select(t => $"        modelBuilder.Entity<{t.Name}>()\r\n\t\t\t.Property(a=> a.Id)\r\n\t\t\t.HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);").ToList();
            var DbSetStrings = DTOTypes.Select(t => $"        public DbSet<{t.Name}> {ps.Pluralize(t.Name)}{{ get; set; }}").ToList();

            var defs = string.Join("\r\n", DbSetStrings);
        }

        static void DownloadVideos(params string[] urls)
        {
            foreach (var url in urls)
            {
                DownloadVideo(url);
            }
        }


        static void PollRTS()
        {
            var url = "Stream_AccountActivity";
            var id = GetId("");
            var t = Tweet.GetTweet(id);
            var ids = Tweet.GetRetweetersIds(t.Id).ToList();
            Action<List<long>> processIds = (rtIds) => { };

            while (true)
            {
                processIds(ids);
                System.Threading.Thread.Sleep(60000);
                var newIds = Tweet.GetRetweetersIds(t.Id);
            }
        }
        private static void TestDb()
        {
            //var ctx = new DbFx.TDbContext();
            DbFx.TDbContext.Test(null);
        }

        private static void monitorLikes()
        {
            SetCreds();
            //https://twitter.com/SuriusVsVodka/status/1150104469281153031
            //       //t.RetweetCount = 4916, t.FavoriteCount = 7802
            long tweetId = 1150104469281153031;
            var t = Tweet.GetTweet(tweetId);
            var rtCount = t.RetweetCount;

            var favCount = t.FavoriteCount;
            long cursor = -1;
            var query = $"https://api.twitter.com/1.1/statuses/retweeters/ids.json?id={tweetId}&cursor={cursor}";


            var queryResult = TwitterAccessor.ExecuteGETQuery<IdsCursorQueryResultDTO>(query);
            List<long> currentIds = queryResult.Results.ToList();
            var ids = new List<long>();
            while (currentIds.Count > 0)
            {
                ids.AddRange(currentIds);
                var first = currentIds.Last();
                cursor = queryResult.NextCursor;
                query = $"https://api.twitter.com/1.1/statuses/retweeters/ids.json?id={tweetId}&count=100&cursor={cursor}";
                queryResult = TwitterAccessor.ExecuteGETQuery<IdsCursorQueryResultDTO>(query);
                currentIds = queryResult.Results.ToList();
            }






            bool go = true;
            while (go)
            {
                System.Threading.Thread.Sleep(10000);
                var updatedIds = Tweet.GetRetweetersIds(t.Id, 10000).ToList(); ;
                var newIds = updatedIds.Except(ids).ToList(); ;
                var removed = ids.Except(newIds).ToList();

            }

        }

        private static void ProcessTL(int? maxTweets = 2000)
        {
            maxTweets = maxTweets ?? 2000;
            //Console.WriteLine(tl.Count);
            var user = User.GetAuthenticatedUser();

            var utlp = new Tweetinvi.Parameters.UserTimelineParameters
            {
                ExcludeReplies = false,
                //FormattedCustomQueryParameters = "",
                IncludeContributorDetails = false,
                IncludeEntities = true,
                IncludeRTS = true,
                MaxId = -1,
                MaximumNumberOfTweetsToRetrieve = 200,
                SinceId = -1
            };

            var all = new List<ITweet>();
            List<ITweet> buffer = null;
            long maxId = -1;
            bool go = true;
            while (go)
            {
                utlp.MaxId = maxId;
                buffer = user.GetUserTimeline(utlp).ToList();
                all.AddRange(buffer);
                var max = buffer.Max(x => x.Id);
                var min = buffer.Min(x => x.Id);
                maxId = min - 1;
                go = all.Count <= maxTweets && buffer.Count > 0;// == utlp.MaximumNumberOfTweetsToRetrieve;
                Console.Title = $"Collected {all.Count}";
            }

            Func<List<IHashtagEntity>, bool> htmatch = (hts) => hts.Any(x => string.Compare(x.Text, "#magachallenge") > -1);
            Func<List<IMediaEntity>, bool> mmatch = (medias) => medias.Any(x => x.MediaType == "video");
            Func<ITweet, bool> chmatch = (it) => htmatch(it.Hashtags) && mmatch(it.Media);
            Func<ITweet, long> mediaId = (it) => it.Media.First(x => x.MediaType == "video").Id.Value;
            Func<ITweet, string> mediaUrl = (it) => it.Media.First(x => x.MediaType == "video").MediaURL;
            Func<ITweet, IMediaEntity> media = (it) => it.Media.First(x => x.MediaType == "video");
            var chl = all.Where(x => chmatch(x)).ToList();


            Dictionary<long, ITweet> chlMedia = chl.ToLookup(x => mediaId(x)).ToDictionary(x => x.Key, x => x.First());




            var mediaDirectory = Directory.CreateDirectory("media");
            var jsonFileName = $"cht-{DateTime.Now.ToString("yyyyMMdd_HHSS")}.json";
            var jsonFile = Path.Combine(mediaDirectory.FullName, jsonFileName);
            File.WriteAllText(jsonFile, chlMedia.ToJson());
            int processed = 0;
            foreach (var kvp in chlMedia)
            {
                Console.Title = $"Processing {++processed} of {chlMedia.Count}";
                var m = media(kvp.Value);
                var id = m.Id;

                var data = TwitterAccessor.DownloadBinary(m.VideoDetails.Variants.First().URL);
                var fileName = $"{id}.{m.VideoDetails.Variants.First().ContentType.Split('/')[1]}";
                var dest = Path.Combine(mediaDirectory.FullName, fileName);
                File.WriteAllBytes(dest, data);
            }



        }

        static string DownloadMedia(ITweet t)
        {
            var m = t.Media.First(x => x.MediaType == "video");
            var variant = m.VideoDetails.Variants.First(x => x.ContentType == "video/mp4");
            var fileName = Path.GetFileName(variant.URL.Split('?')[0]);
            var mediaDirectory = Directory.CreateDirectory("Media");
            var dest = Path.Combine(mediaDirectory.FullName, fileName);
            if (File.Exists(dest))
            {
                return dest;
            }
            var id = t.Id;

            var data = TwitterAccessor.DownloadBinary(variant.URL);
            //var fileName = $"{id}.{m.VideoDetails.Variants.First(x=> x.ContentType== "video/mp4").ContentType.Split('/')[1]}";


            File.WriteAllBytes(dest, data);
            Console.WriteLine("Saved to {0}", fileName);
            return dest;
        }

        static bool credsAreSet = false;
        static void SetCreds()
        {
            if (credsAreSet) return;
            credsAreSet = true;
            //Auth.SetUserCredentials("CONSUMER_KEY", "CONSUMER_SECRET", "ACCESS_TOKEN", "ACCESS_TOKEN_SECRET");
            Auth.SetUserCredentials(Creds.Settings.CONSUMER_KEY, Creds.Settings.CONSUMER_SECRET,
                Creds.Settings.ACCESS_TOKEN, Creds.Settings.ACCESS_TOKEN_SECRET);
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
            // Publish the Tweet "Hello World" on your Timeline
            //Tweet.PublishTweet("Hello World!");

        }

        static void UpdateFollowing()
        {
           
            FollowHelper.Update();

        }
        public class FollowHelper
        {
            RelHelper helper;
            DbFx.DbRepo repo;
            public FollowHelper()
            {
                helper = new RelHelper();
                // System.Diagnostics.Debug.WriteLine("Refreshing relationships from api");
                //helper.RefreshFromApi();
                //helper.GetFollowers();
                //helper.GetFriends();
                //System.Diagnostics.Debug.WriteLine("Refreshed relationships from api");

                repo = new Examplinvi.DbFx.DbRepo();

                //var apiUsersIFollow = helper.Friends;
                //var apiUsersFollowingMe = helper.Followers;

                //System.Diagnostics.Debug.WriteLine("Refreshing relationships from db");
                //var dbUsersIFollow = repo.UsersIFollow();
                //var dbUsersFollowingMe = repo.UsersFollowingMe();
                //System.Diagnostics.Debug.WriteLine("Refreshed relationships from db");
            }


            private void Run()
            {
                var followersComparer = new FollowersComparer(repo, helper);
                followersComparer.Update();
                var followingComparer = new FollowingComparer(repo, helper);
                followingComparer.Update();
            }

            public static void Update()
            {
                var helper = new FollowHelper();
                helper.Run();
            }
        }
        public class FollowingComparer
        {
            List<DbUser> dbUsers;
            List<IUser> apiUsers;
            List<long> dbIds;
            List<long> apiIds;
            Dictionary<long, DbUser> dbDict;
            Dictionary<long, IUser> apiDict;
            public FollowingComparer(DbFx.DbRepo repo, RelHelper helper) : this(repo.UsersIFollow(), helper.Friends) { }
            public FollowingComparer(List<DbUser> dbFollowing, List<IUser> apiFollowing)
            {
                this.dbUsers = dbFollowing;
                this.dbIds = dbUsers.Select(x => x.Id).ToList();
                dbDict = dbUsers.ToDictionary(x => x.Id, x => x);

                this.apiUsers = apiFollowing;
                this.apiIds = apiUsers.Select(x => x.Id).ToList();
                this.apiDict = apiUsers.ToDictionary(x => x.Id, x => x);
            }

            public List<DbUser> FollowingThatHaventFollowedBack()
            {
                var now = DateTime.Now;
                var result = dbUsers.Where(x =>
                        x.Following == true
                        && x.FollowedDate != null
                        && x.WhiteListed == false
                        && now.Subtract(x.FollowedDate.Value).TotalDays > 3)
                        .ToList();
                return result;
            }
            public void AutoUnfollow()
            {
                var unfollowList = FollowingThatHaventFollowedBack();
                var repo = new DbFx.DbRepo();
                int unfollowCount = 0;
                foreach (var u in unfollowList)
                {
                    Console.WriteLine($"Unfollowing {++unfollowCount} of {unfollowList.Count}");
                    User.UnFollowUser(u.Id);
                    u.Following = false;
                    u.UnFollowedDate = DateTime.Now;
                    repo.Update(u);
                }
            }

            public void Update()
            {
                AutoUnfollow();
            }

        }

        public class FollowersComparer
        {
            List<DbUser> dbUsers;
            List<IUser> apiUsers;
            List<long> dbIds;
            List<long> apiIds;
            Dictionary<long, DbUser> dbDict;
            Dictionary<long, IUser> apiDict;

            public FollowersComparer(DbFx.DbRepo repo, RelHelper helper) : this(repo.UsersFollowingMe(), helper.Followers) { }
            public FollowersComparer(List<DbUser> dbFollowers, List<IUser> apiFollowers)
            {
                this.dbUsers = dbFollowers;
                this.dbIds = dbUsers.Select(x => x.Id).ToList();
                dbDict = dbUsers.ToDictionary(x => x.Id, x => x);

                this.apiUsers = apiFollowers;
                this.apiIds = apiUsers.Select(x => x.Id).ToList();
                this.apiDict = apiUsers.ToDictionary(x => x.Id, x => x);
            }
            public List<IUser> NewApiUsersFollowingMe()
            {
                var result = apiUsers.Where(x => !dbIds.Contains(x.Id)).ToList();

                return result;
            }
            public List<DbUser> NewDbUsersFollowingMe()
            {
                var result = dbUsers.Where(x => x.FollowsMe == false && apiDict.ContainsKey(x.Id)).ToList();

                return result;
            }
            public List<DbUser> DbUsersThatUnfollowedMe()
            {
                var result = dbUsers.Where(x => x.FollowsMe == true && !apiIds.Contains(x.Id)).ToList();

                return result;
            }
            public void Update()
            {
                AddNewFollows();
                UpdateExistingNewFollowers();
                UpdateExistingThatStoppedFollowing();
            }

            private void AddNewFollows()
            {
                var dbUsers = NewApiUsersFollowingMe().Select(x => x.ToDbUser(true)).ToList();
                var repo = new DbFx.DbRepo();
                var newDbUserIds = dbUsers.Select(x => x.Id).ToList();
                
                var inDb = repo.Context.Users.Where(x => newDbUserIds.Contains(x.Id)).ToList();
                var inDbIds = inDb.Select(x => x.Id).ToList();

                repo.Add((IEnumerable<DbUser>)dbUsers.Where(x=> !inDbIds.Contains(x.Id)));
                inDb.ForEach(x =>
                {
                    x.FollowsMe = true;
                    x.FollowedMeDate = DateTime.Now;
                });
                repo.Update((IEnumerable<DbUser>)inDb);
            }

            private void UpdateExistingNewFollowers()
            {
                var dbUsers = NewDbUsersFollowingMe();
                dbUsers.ForEach(x => { x.FollowsMe = true; x.FollowedMeDate = DateTime.Now; });
                var repo = new DbFx.DbRepo();
                repo.Update((IEnumerable<DbUser>)dbUsers);
            }

            private void UpdateExistingThatStoppedFollowing()
            {
                var dbUsers = DbUsersThatUnfollowedMe();
                var idsToUnfollow = new List<long>();
                dbUsers.ForEach(x =>
                {
                    x.FollowsMe = false;

                    if (x.Following && !x.WhiteListed)
                    {
                        x.UnFollowedMeDate = DateTime.Now;
                        idsToUnfollow.Add(x.Id);
                        x.UnFollowedDate = DateTime.Now;
                    }
                });
                var repo = new DbFx.DbRepo();

                repo.Update((IEnumerable<DbUser>)dbUsers);
                int unfollowCount = 0;
                idsToUnfollow.ForEach(id =>
                {
                    Console.WriteLine($"Unfollowing {+unfollowCount} of {idsToUnfollow.Count}");
                    User.UnFollowUser(id);
                    System.Threading.Thread.Sleep(5000);
                });
            }
        }

        static Random Rand = new Random();
        static int RandomizedWait(int min = 5000, int max = 60000) => Rand.Next(min, max);
        static void AutoUnfollow(int maxUnfollowCount = 250)
        {


            var helper = new RelHelper();
            helper.RefreshFromApi();
            var nf = helper.GetFriendsNotFollowing();
            File.WriteAllText($"{nameof(RelHelper.GetFriendsNotFollowing)}.json", nf.ToJson());


            var csvLines = nf.Select(x => $"{x.IdStr}\t{x.ScreenName}\t{0}\t{x.FollowersCount}\t{x.Name.Replace("\t", "").Replace("\r", "").Replace("\n", "")}\t{x.Status.Text.Replace("\t", "").Replace("\r", "").Replace("\n", "")}\t{x.Description.Replace("\t", "").Replace("\r", "").Replace("\n", "")}");
            var csvHeaders = $"Id\tHandle\tWhiteList\tFollowersCount\tName\tStatusText\tDescription";
            File.WriteAllText($"{nameof(RelHelper.GetFriendsNotFollowing)}.txt", $"{csvHeaders}\r\n{string.Join("\r\n", csvLines)}");

            var lastI = 0;
            var lastFollowerCount = 0;
            var authUser = User.GetAuthenticatedUser();
            Dictionary<long, DateTime> unfollows = null;
            unfollows = JsonData.LoadFileOrDefault<Dictionary<long, DateTime>>($"{nameof(unfollows)}.json");
            var now = DateTime.Now;
            unfollows = unfollows.Where(x => now.Subtract(x.Value).TotalHours < 24).ToDictionary(x => x.Key, x => x.Value);
            maxUnfollowCount -= unfollows.Count;
            try
            {
                while (lastI < maxUnfollowCount && lastI < nf.Count && lastFollowerCount < 2500)
                {
                    var f = nf[lastI++];
                    lastFollowerCount = f.FollowersCount;
                    helper.Unfollow(f);
                    unfollows.Add(f.Id, DateTime.Now);
                    Console.Title = $"Unfollowed {lastI} {f.Name} ({lastFollowerCount} followers)";

                    System.Threading.Thread.Sleep(RandomizedWait());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (lastI > 1)
                {
                    helper.SaveFriends();
                    File.WriteAllText($"{nameof(unfollows)}.json", unfollows.ToJson());
                }

            }

            var user = User.GetAuthenticatedUser();
            // Tweetinvi will now wait for tokens to be available before performing a request.
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;

            // Get ALL the friends' ids of a specific user
            //var friendsIds = User.GetFriendIds(< user_identifier >, Int32.MaxValue);




            var friends = user.GetFriends(Int32.MaxValue);
            var friendIds = friends.Select(x => x.Id).ToList();

            var followers = user.GetFollowers(Int32.MaxValue);
            var followerIds = followers.Select(x => x.Id).ToList();
            var friendsLookup = friends.ToLookup(x => x.Id);
            var followersLookup = followers.ToLookup(x => x.Id);

            var friendsThatFollowIds = friendIds.Union(followerIds).ToList();

            var friendsThatDontFollowIds = friendIds.Except(followerIds).ToList();


            var friendsThatFollow = friendsLookup
                .Where(x => friendsThatFollowIds.Contains(x.Key)).SelectMany(x => x).ToList();
            var friendsThatDontFollow = friendsLookup
              .Where(x => !friendsThatFollowIds.Contains(x.Key)).SelectMany(x => x).ToList();







            System.IO.File.WriteAllText($"{nameof(friendIds)}.json", friendIds.ToJson());
            System.IO.File.WriteAllText($"{nameof(friends)}.json", friends.ToJson());
            System.IO.File.WriteAllText($"{nameof(followers)}.json", followers.ToJson());
            System.IO.File.WriteAllText($"{nameof(followerIds)}.json", followerIds.ToJson());
            System.IO.File.WriteAllText($"{nameof(friendsThatFollowIds)}.json", friendsThatFollowIds.ToJson());
            System.IO.File.WriteAllText($"{nameof(friendsThatDontFollowIds)}.json", friendsThatDontFollowIds.ToJson());
            System.IO.File.WriteAllText($"{nameof(friendsThatFollow)}.json", friendsThatFollow.ToJson());
            System.IO.File.WriteAllText($"{nameof(friendsThatDontFollow)}.json", friendsThatDontFollow.ToJson());

        }
    }
}

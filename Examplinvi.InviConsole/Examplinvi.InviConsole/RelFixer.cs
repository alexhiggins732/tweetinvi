using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tweetinvi.Models;
using Tweetinvi;
using Examplinvi.DbFx.Models;
using Examplinvi.DbFx;

namespace Examplinvi.InviConsole
{

    public class RelationShipPair
    {
        public long Id;
        public bool InApi;
        public bool InDb;
        public RelationShipPair(long id, bool inApi, bool inDb)
        {
            this.Id = id;
            this.InApi = inApi;
            this.InDb = inDb;
        }
    }

    public class RelSync
    {
        DbRepo repo = new DbRepo();
        IAuthenticatedUser currentUser = User.GetAuthenticatedUser();
        public RelSync()
        {


        }

        List<long> dbFriendIds;
        List<long> apiFriendIds;
        List<long> dbFollowerIds;
        List<long> apiFollowerIds;
        public List<long> DbFriendIds => dbFriendIds ?? (dbFriendIds = repo.Context.Users.Where(x => x.Following == true).Select(x => x.Id).ToList());
        public List<long> ApiFriendIds => apiFriendIds ?? (apiFriendIds = currentUser.GetFriendIds().ToList());

        public List<long> DbFollowerIds => dbFollowerIds ?? (dbFollowerIds = repo.Context.Users.Where(x => x.FollowsMe == true).Select(x => x.Id).ToList());
        public List<long> ApiFollowerIds => apiFollowerIds ?? (apiFollowerIds = currentUser.GetFollowerIds(Int32.MaxValue).ToList());


        public void Run()
        {

            apiFriendIds = dbFriendIds = dbFollowerIds = apiFollowerIds = null;
            var changedFriends = DbFriendIds
                .Concat(ApiFriendIds)
                .Distinct()
                .Select(id => new RelationShipPair(id, ApiFriendIds.Contains(id), DbFriendIds.Contains(id)))
                .Where(pair => !pair.InApi || !pair.InDb)
                .ToList();
            ProcessChangedFriends(changedFriends);

            var changedFollowers = DbFollowerIds
             .Concat(ApiFollowerIds)
             .Distinct()
             .Select(id => new RelationShipPair(id, ApiFollowerIds.Contains(id), DbFollowerIds.Contains(id)))
             .Where(pair => !pair.InApi || !pair.InDb)
             .ToList();

            ProcessChangedFollowers(changedFollowers);


        }

        public void AutoUnfollow()
        {
            int processedCount = 0;

            List<DbUser> unfollowList = new List<DbUser>();
            var limit = DateTime.Now.AddDays(-3);
            using (var ctx = new DbFx.TDbContext())
            {
                unfollowList = ctx.Users.Where(x => x.Following == true && x.FollowedDate <limit && x.FollowsMe == false && x.WhiteListed == false).ToList();
            }
            int total = unfollowList.Count;
            Console.WriteLine($"{nameof(AutoUnfollow)}: Processing {total} auto unfollows");
            foreach (var autoUnfollow in unfollowList)
            {
                Console.WriteLine($"{nameof(AutoUnfollow)}: Processed {++processedCount} of {total}");
                using (var repo = new DbRepo())
                {
                    autoUnfollow.UnFollowedDate = DateTime.Now;
                    autoUnfollow.Following = false;
                    repo.Update(autoUnfollow);
                    bool unfollowed = currentUser.UnFollowUser(autoUnfollow.Id);
                    if (!unfollowed)
                    {
                        var apiUser = User.GetUserFromId(autoUnfollow.Id);
                        unfollowed = !(bool)(apiUser?.Following ?? false);
                        if (!unfollowed)
                        {
                            string bp = "";
                        }
                    }
                }

            }
        }

        private void ProcessChangedFollowers(List<RelationShipPair> changedFollowers)
        {
            int processedCount = 0;
            int total = changedFollowers.Count;
            Console.WriteLine($"{nameof(ProcessChangedFollowers)}: Processing {total} changed followers");
            List<DbUser> unfollowList = new List<DbUser>();
            List<long> newFollowerIds = changedFollowers.Where(x => !x.InDb).Select(x => x.Id).ToList();
            Dictionary<long, IUser> newFollowers = User.GetUsersFromIds(newFollowerIds).ToDictionary(x => x.Id, x => x);
            foreach (var changedFollower in changedFollowers)
            {
                Console.WriteLine($"{nameof(ProcessChangedFollowers)}: Processed {++processedCount} of {total}");
                if (!changedFollower.InApi) // user unfollowed
                {
                    var dbUser = repo.GetUserById(changedFollower.Id);
                    var screenName = dbUser.ScreenName;
                    dbUser.FollowsMe = false;
                    dbUser.UnFollowedMeDate = DateTime.Now;
                    if (dbUser.Following && !dbUser.WhiteListed) // unfollow back if following and user is not whitelisted.
                    {
                        unfollowList.Add(dbUser);
                    }
                    repo.Update(dbUser);
                }
                if (!changedFollower.InDb) // user followed
                {

                    //var apiUser = User.GetUserFromId(changedFollower.Id);
                    var apiUser = newFollowers[changedFollower.Id];
                    var screenName = apiUser.ScreenName;
                    var dbUser = apiUser.ToDbUser(true);
                    try
                    {
                        repo.Add(dbUser);
                    }
                    catch (Exception ex)
                    {
                        repo = new DbRepo();
                        dbUser = repo.GetUserById(changedFollower.Id);
                        dbUser.FollowsMe = true;
                        dbUser.FollowedMeDate = DateTime.Now;
                        repo.Update(dbUser);
                    }

                }
            }
            ProcessUnfollows(unfollowList);

        }

        private void ProcessUnfollows(List<DbUser> unfollowList)
        {
            int processedCount = 0;
            int total = unfollowList.Count;
            Console.WriteLine($"{nameof(ProcessUnfollows)}: Processing {total} auto unfollows");
            while (unfollowList.Count > 0)
            {
                Console.WriteLine($"{nameof(ProcessUnfollows)}: Processed {++processedCount} of {total}");
                var current = unfollowList.First();
                unfollowList.RemoveAt(0);
                bool unfollowed = User.UnFollowUser(current.Id);
                if (!unfollowed)
                {
                    var apiUser = User.GetUserFromId(current.Id);
                    unfollowed = !(bool)(apiUser?.Following ?? false);
                    if (!unfollowed)
                    {
                        string bp = "";
                    }
                }
            }
        }

        private void ProcessChangedFriends(List<RelationShipPair> changedFriends)
        {
            int processedCount = 0;
            int total = changedFriends.Count;
            Console.WriteLine($"{nameof(ProcessChangedFriends)}: Processing {total}  changed friends");
            int iUnfollowed = 0;
            int iFollowed = 0;
            var lastIUnfollowed = 0;
            var lastIFollowed = 0;
            var newFollowedIds = changedFriends.Where(x => !x.InDb).Select(x => x.Id).ToList();
            var newFollowers = User.GetUsersFromIds(newFollowedIds).ToDictionary(x => x.Id, x => x);
            foreach (var friend in changedFriends)
            {
                Console.WriteLine($"{nameof(ProcessChangedFriends)}: Processed {++processedCount} of {total}");
                if (!friend.InApi) // I unfollowed
                {
                    var dbUser = repo.GetUserById(friend.Id);
                    dbUser.Following = false;
                    dbUser.UnFollowedDate = DateTime.Now;
                    repo.Update(dbUser);
                    lastIUnfollowed = iUnfollowed;
                    iUnfollowed++;
                }
                if (!friend.InDb) // I followed
                {
                    //var user = User.GetUserFromId(friend.Id);
                    var user = newFollowers[friend.Id];
                    var dbUser = user.ToDbUser();
                    dbUser.Following = true;
                    dbUser.FollowedDate = DateTime.Now;
                    try
                    {
                        repo.Add(dbUser);
                    }
                    catch (Exception ex)
                    {
                        repo = new DbRepo();
                        dbUser = repo.GetUserById(friend.Id);
                        dbUser.Following = true;
                        dbUser.FollowedDate = DateTime.Now;
                        repo.Update(dbUser);
                    }
                    lastIFollowed = iFollowed;
                    iFollowed++;
                }
                if((iFollowed!=lastIFollowed && iFollowed%100==0) || (lastIUnfollowed!=iUnfollowed && lastIUnfollowed % 100 == 0))
                {
                    Console.WriteLine($"{DateTime.Now}: Followed:{iFollowed} UnFollowed:{iUnfollowed}");
                }
            }
        }
    }
    public class RelFixer
    {
        static readonly DirectoryInfo previousRoot =
            new DirectoryInfo(@"C:\Source\Repos\tweetinvi\Examplinvi.InviConsole\Examplinvi.InviConsole\bin\Debug\rels_back_20190927");
        static readonly DirectoryInfo currentRoot =
        new DirectoryInfo(@"C:\Source\Repos\tweetinvi\Examplinvi.InviConsole\Examplinvi.InviConsole\bin\Debug\");

        static List<IUser> previousIFollowed()
        {
            var json = File.ReadAllText(Path.Combine(previousRoot.FullName, "friends.json"));
            var result = JsonSerializer.ConvertJsonTo<List<IUser>>(json);
            return result;
        }

        static List<IUser> previousFollowedMe()
        {
            var json = File.ReadAllText(Path.Combine(previousRoot.FullName, "followers.json"));
            var result = JsonSerializer.ConvertJsonTo<List<IUser>>(json);
            return result;
        }


        static List<IUser> IFollowed()
        {
            var json = File.ReadAllText(Path.Combine(currentRoot.FullName, "friends.json"));
            var result = JsonSerializer.ConvertJsonTo<List<IUser>>(json);
            return result;
        }

        static List<IUser> FollowedMe()
        {
            var json = File.ReadAllText(Path.Combine(currentRoot.FullName, "followers.json"));
            var result = JsonSerializer.ConvertJsonTo<List<IUser>>(json);
            return result;
        }


        static void RevertPrevious()
        {
            var prevIFollowed = previousIFollowed();
            var prevIFollowedIds = prevIFollowed.Select(x => x.Id).ToList();
            var prevFollowedMe = previousFollowedMe();
            var prevFollowedMeIds = prevFollowedMe.Select(x => x.Id).ToList();
            DateTime? startDate = DateTime.Parse("9/21/2019");
            var allIds = prevIFollowedIds.Concat(prevFollowedMeIds).Distinct().ToList();
            using (var ctx = new DbFx.TDbContext())
            {
                int count = 0;
                var users = ctx.Users.Where(x => allIds.Contains(x.Id)).ToList();
                users.ForEach(x =>
                {
                    x.Following = prevIFollowedIds.Contains(x.Id);
                    x.FollowsMe = prevFollowedMeIds.Contains(x.Id);
                    x.FollowedDate = x.Following ? startDate : null;
                    x.FollowedMeDate = x.FollowsMe.Value ? startDate : null;
                    x.UnFollowedDate = null;
                    x.UnFollowedMeDate = null;
                    ctx.Users.Attach(x);
                    ctx.Entry(x).State = System.Data.Entity.EntityState.Modified;
                    if ((++count) % 100 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Updated {0} of {users.Count}");
                    }
                });
                System.Diagnostics.Debug.WriteLine($"Updating {users.Count} records");
                int updated = ctx.SaveChanges();
                System.Diagnostics.Debug.WriteLine($"Updating {updated} records");
            }
        }
        static void Update()
        {
            var iFollow = IFollowed();
            var iFollowIds = iFollow.Select(x => x.Id).ToList();
            var followsMe = FollowedMe();
            var followsMeIds = followsMe.Select(x => x.Id).ToList();
            DateTime? startDate = DateTime.Parse("9/22/2019");

            List<long> IdsIUnfollowed = new List<long>();
            List<long> IdsIFollowed = new List<long>();
            List<long> IdsThatUnfollowedMe = new List<long>();
            List<long> IdsThatFollowedMe = new List<long>();
            List<DbUser> users = null;
            List<DbUser> changes = new List<DbUser>();
            using (var ctx = new DbFx.TDbContext())
            {
                users = ctx.Users.ToList();
                users.ForEach(x =>
                {
                    var prevFollowing = x.Following;
                    x.Following = iFollowIds.Contains(x.Id);
                    bool dirty = false;
                    if (!x.Following == prevFollowing)//I Follow them changed.
                    {
                        dirty = true;
                        if (!x.Following) // I no longer follow.
                        {
                            IdsIUnfollowed.Add(x.Id);
                            x.UnFollowedDate = startDate;
                        }
                        else // I started following
                        {
                            IdsIFollowed.Add(x.Id);
                            x.FollowedDate = startDate;
                        }
                    }
                    var prevFollowsMe = x.FollowsMe ?? false;
                    x.FollowsMe = followsMeIds.Contains(x.Id);
                    if (!x.FollowsMe == prevFollowsMe) // follows me changed
                    {
                        dirty = true;
                        if (!x.FollowsMe.Value) // stopped follwoing me.
                        {
                            IdsThatUnfollowedMe.Add(x.Id);
                            x.UnFollowedMeDate = startDate;
                        }
                        else // started
                        {
                            IdsThatFollowedMe.Add(x.Id);
                            x.FollowedMeDate = startDate;
                        }
                    }
                    //ctx.Users.Attach(x);
                    //ctx.Entry(x).State = System.Data.Entity.EntityState.Modified;
                    if (dirty)
                        changes.Add(x);
                });

                List<long> IdsToUnfollow = new List<long>();
                DateTime UnfollowDate = DateTime.Now;
                changes.ForEach(x =>
                {
                    if (IdsIUnfollowed.Contains(x.Id))
                    {

                    }
                    if (IdsIFollowed.Contains(x.Id))
                    {
                        if (!IdsThatFollowedMe.Contains(x.Id))
                        {
                            IdsToUnfollow.Add(x.Id);
                            x.UnFollowedDate = UnfollowDate;
                        }
                    }
                    if (IdsThatFollowedMe.Contains(x.Id))
                    {

                    }
                    if (IdsThatUnfollowedMe.Contains(x.Id))
                    {
                        if (IdsIFollowed.Contains(x.Id))
                        {
                            IdsToUnfollow.Add(x.Id);
                            x.UnFollowedDate = UnfollowDate;
                        }
                    }

                    ctx.Users.Attach(x);
                    ctx.Entry(x).State = System.Data.Entity.EntityState.Modified;
                });

                ctx.SaveChanges();
                int unfollowedCount = 0;
                Creds.Helper.SetCreds();
                IdsToUnfollow.Distinct().ToList().ForEach(id =>
                {
                    Console.WriteLine($"Unfollowing {++unfollowedCount} of {IdsToUnfollow.Count}");
                    User.UnFollowUser(id);
                });
            }


        }
        public static void run()
        {
            VerifyFriends();
            //RevertPrevious();
            Update();
            AutoUnfollow();
        }

        public static void VerifyFriends()
        {
            Creds.Helper.SetCreds();
            var newIds = User.GetAuthenticatedUser().GetFriendIds(5000);
            List<DbUser> dbUsers = null;
            using (var ctx = new DbFx.TDbContext())
            {
                dbUsers = ctx.Users.Where(x => x.Following == true).ToList();
            }
            var dbUserIds = dbUsers.Select(x => x.Id).ToList();

            var FollowingButNotInDb = newIds.Except(dbUserIds).ToList();
            List<DbUser> missingDbIds = null;
            using (var ctx = new DbFx.TDbContext())
            {
                missingDbIds = ctx.Users.Where(x => FollowingButNotInDb.Contains(x.Id)).ToList();
            }

            int count = missingDbIds.Count;

        }
        public static void AutoUnfollow()
        {
            Creds.Helper.SetCreds();
            List<DbUser> queue = null;
            using (var ctx = new DbFx.TDbContext())
            {
                queue = ctx.Users.Where(x =>
                x.FollowersCount < 10000
                && x.WhiteListed == false
                && x.Following == true
                && x.FollowsMe == false).ToList();
            }
            int count = 0;
            while (queue.Count > 0)
            {
                var first = queue.First();
                queue.RemoveAt(0);
                User.UnFollowUser(first.Id);

                using (var ctx = new DbFx.TDbContext())
                {
                    first.Following = false;
                    first.UnFollowedDate = DateTime.Now;
                    ctx.Users.Attach(first);
                    ctx.Entry(first).State = System.Data.Entity.EntityState.Modified;
                    int updated = ctx.SaveChanges();
                    System.Threading.Thread.Sleep(5000);
                    Console.WriteLine($"Unfollowed {++count}. {queue.Count} remaining");
                }
            }


        }
    }
}

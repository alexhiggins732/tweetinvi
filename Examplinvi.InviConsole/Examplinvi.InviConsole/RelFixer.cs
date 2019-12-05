using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tweetinvi.Models;
using Tweetinvi;
using Examplinvi.DbFx.Models;

namespace Examplinvi.InviConsole
{
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
                dbUsers = ctx.Users.Where(x => x.Following==true).ToList();
            }
            var dbUserIds = dbUsers.Select(x => x.Id).ToList();

            var FollowingButNotInDb = newIds.Except(dbUserIds).ToList();
            List<DbUser> missingDbIds = null;
            using (var ctx = new DbFx.TDbContext())
            {
                missingDbIds = ctx.Users.Where(x => FollowingButNotInDb.Contains(x.Id) ).ToList();
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

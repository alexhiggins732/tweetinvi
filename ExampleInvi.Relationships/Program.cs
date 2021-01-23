using Examplinvi.DbFx.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
using System.Timers;
using Tweetinvi;

namespace Examplinvi
{
    class Program
    {
        static Program()
        {
            Creds.Helper.SetCreds();
        }

        static ConcurrentQueue<long> UnfollowQueue = null;
        static Timer UnfollowTimer = null;
        static void Main(string[] args)
        {
            UnfollowQueue = new ConcurrentQueue<long>();
            UnfollowTimer = new Timer();
            UnfollowTimer.AutoReset = true;
            UnfollowTimer.Interval = 10000;
            UnfollowTimer.Elapsed += UnfollowTimer_Elapsed;
            UnfollowedComplete = false;

            SyncRelationships();

            while (UnfollowedComplete == false || UnfollowQueue.Count > 0)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        static bool UnfollowedComplete = false;

        static void MarkUnfollowed(long id)
        {
            using (var repo = new DbFx.DbRepo())
            {
                var user = repo.Context.Users.First(x => x.Id == id);
                user.FollowsMe = false;
                user.UnFollowedDate = DateTime.Now;
                repo.Update(user);
            }
        }
        private static void UnfollowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (UnfollowQueue.Count > 0)
            {
                LogDebug($"Unfollowing: {UnfollowQueue.Count}");
                if (UnfollowQueue.TryDequeue(out long id))
                {
                    var u = User.GetUserFromId(id);
                    LogDebug($"Unfollowing: {(u?.ScreenName ?? "null")} - Following = {(u?.Following ?? false)}");
                    if (u == null)
                    {
                        MarkUnfollowed(id);
                        return;
                    }
                    bool unfollowed = User.UnFollowUser(id);
                    if (unfollowed)
                    {
                        LogDebug($"Unfollowed: {(u.ScreenName ?? "null")}");
                        MarkUnfollowed(id);
                    }
                    else
                    {

                        LogDebug($"Requeue unfollow: {(u.ScreenName ?? "null")}");
                        UnfollowQueue.Enqueue(id);
                    }
                    if (UnfollowQueue.Count > 0)
                    {
                        System.Threading.Thread.Sleep(5000);
                    }
                }

            }
            else
            {
                UnfollowedComplete = true;
                UnfollowTimer.Stop();
                UnfollowTimer.Elapsed -= UnfollowTimer_Elapsed;
            }
        }

        private static void LogDebug(string v)
        {
            string message = $"{DateTime.Now}: {v}";
            Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(message);
        }

        private static void SyncRelationships()
        {
            LogDebug($"{ nameof(SyncRelationships)}");
            var authenticatedUser = User.GetAuthenticatedUser();

            //sync followers
            LogDebug($"{ nameof(authenticatedUser.GetFollowerIds)}");
            var followerIds = authenticatedUser.GetFollowerIds(int.MaxValue).ToList();
            LogDebug($"{nameof(followerIds)} = {followerIds.Count}");

            LogDebug($"{ nameof(authenticatedUser.GetFriendIds)}");
            var friendIds = authenticatedUser.GetFriendIds(int.MaxValue).ToList();
            if(followerIds.Count==0 || friendIds.Count == 0)
            {
                LogDebug($"Exiting due to api error");
                return;
            }
            LogDebug($"{nameof(friendIds)} = {friendIds.Count}");

            AssureDbUsers(followerIds, friendIds);

            SyncFollowers(followerIds);
            SyncFriends(friendIds);
        }

        private static void AssureDbUsers(List<long> followerIds, List<long> friendIds)
        {
            var allApiIds = followerIds.Concat(friendIds).Distinct().ToList();
            LogDebug($"Processing {nameof(allApiIds)} = {allApiIds.Count}");
            List<DbUser> dbUsers = null;
            using (var repo = new DbFx.DbRepo())
            {
                dbUsers = repo.Context.Users.ToList();
            }
            LogDebug($"found {nameof(dbUsers)} = {dbUsers.Count}");
            var dbIds = dbUsers.ToDictionary(x => x.Id, x => x);
            var allApiIdsNotInDb = allApiIds.Where(x => !dbIds.ContainsKey(x)).ToList();
            LogDebug($"Processing {nameof(allApiIdsNotInDb)} = {allApiIdsNotInDb.Count}");
            var apiUsers = User.GetUsersFromIds(allApiIdsNotInDb).ToList();
            LogDebug($"Api returned {nameof(apiUsers)} = {apiUsers.Count}");
            var newDbUsers = apiUsers.Select(x => x.ToDbUser()).ToList();
            newDbUsers.ForEach(x =>
            {
                if (followerIds.Contains(x.Id))
                {
                    x.FollowsMe = true;
                    x.FollowedMeDate = DateTime.Now;
                }
                if (friendIds.Contains(x.Id))
                {
                    x.Following = true;
                    x.FollowedDate = DateTime.Now;
                }
            });
            LogDebug($"Saving {nameof(newDbUsers)} = {newDbUsers.Count}");
            using (var repo = new DbFx.DbRepo())
            {
                repo.Add(newDbUsers);
            }
        }

        private static void SyncFollowers(List<long> followerIds)
        {
            LogDebug($"{ nameof(SyncFollowers)}");
            List<long> dbFollowerIds = null;
            using (var repo = new DbFx.DbRepo())
            {
                dbFollowerIds = repo.Context.Users.Where(x => x.FollowsMe == true).Select(x => x.Id).ToList();
            }
            LogDebug($"Found {nameof(dbFollowerIds)} = {dbFollowerIds.Count}");
            LogDebug($"Processing {nameof(followerIds)} = {followerIds.Count}");
            var followersInDbButNotInApi = dbFollowerIds.Except(followerIds).ToList();
            LogDebug($"Found {nameof(followersInDbButNotInApi)} = {followersInDbButNotInApi.Count}");

            var followersInApiButNotInDb = followerIds.Except(dbFollowerIds).ToList();
            LogDebug($"Found {nameof(followersInDbButNotInApi)} = {followersInDbButNotInApi.Count}");


            RemoveFollowers(followersInDbButNotInApi);
            AddApiFollowers(followersInApiButNotInDb);
        }

        private static void RemoveFollowers(List<long> inDbButNotInApi)
        {
            LogDebug($"{nameof(RemoveFollowers)}");
            var repo = new DbFx.DbRepo();
            LogDebug($"{nameof(RemoveFollowers)} - Processing : {nameof(inDbButNotInApi)}  = {inDbButNotInApi.Count}");
            var users = repo.Context.Users.Where(x => inDbButNotInApi.Contains(x.Id)).ToList();
            users.ForEach(x =>
            {
                if (DateTime.Now.Subtract(x.FollowedDate ?? DateTime.MinValue).TotalHours>72)
                    UnfollowQueue.Enqueue(x.Id);
            });
            if (UnfollowQueue.Count > 0)
            {

                UnfollowedComplete = false;
                UnfollowTimer.Start();
                System.Threading.Thread.Sleep(1000);
                LogDebug($"Started {nameof(UnfollowTimer)}");
            }
            else
            {
                UnfollowedComplete = true;
                LogDebug($"Skipped {nameof(UnfollowTimer)}");
            }



        }

        private static void AddApiFollowers(List<long> inApiButNotInDb)
        {
            LogDebug($"{ nameof(AddApiFollowers)}");
            var apiFollowers = User.GetUsersFromIds(inApiButNotInDb);
            var apiDtos = apiFollowers.Select(x => x.ToDbUser(true)).ToList();
            var repo = new DbFx.DbRepo();
            repo.Update(apiDtos);

        }

        private static void SyncFriends(List<long> apiFriendIds)
        {
            LogDebug($"{ nameof(SyncFriends)}");
            List<long> dbFriendIds = null;
            using (var repo = new DbFx.DbRepo())
            {
                dbFriendIds = repo.Context.Users.Where(x => x.Following == true).Select(x => x.Id).ToList();
            }
            LogDebug($"Found {nameof(dbFriendIds)} = {dbFriendIds.Count}");
            LogDebug($"Processing {nameof(apiFriendIds)} = {apiFriendIds.Count}");

            var inApiButNotInDb = apiFriendIds.Except(dbFriendIds).ToList();
            LogDebug($"{nameof(inApiButNotInDb)} = {inApiButNotInDb.Count}");


            var inDbButNotInApi = dbFriendIds.Except(apiFriendIds).ToList();
            LogDebug($"{nameof(inDbButNotInApi)} = {inDbButNotInApi.Count}");


            RemoveFriendsFromDb(inDbButNotInApi);
            AddFriendsToDb(inApiButNotInDb);
        }

        private static void RemoveFriendsFromDb(List<long> inDbButNotInApi)
        {
            LogDebug($"{ nameof(RemoveFriendsFromDb)}");
            var repo = new DbFx.DbRepo();
            var friendsInDb = repo.Context.Users.Where(x => inDbButNotInApi.Contains(x.Id)).ToList();
            LogDebug($"Found {nameof(friendsInDb)} =  {friendsInDb.Count} in not whitelisted");
            friendsInDb.ForEach(x =>
            {
                x.Following = false;
                x.UnFollowedDate = DateTime.Now;
            });
            repo.Update(friendsInDb);
        }

        private static void AddFriendsToDb(List<long> inApiButNotInDb)
        {
            LogDebug($"{ nameof(AddFriendsToDb)}");
            List<DbUser> dbUsersNotMarkedAsFriends = null;
            using (var repo = new DbFx.DbRepo())
            {
                dbUsersNotMarkedAsFriends = repo.Context.Users.Where(x => inApiButNotInDb.Contains(x.Id)).ToList();
                LogDebug($"Found {nameof(dbUsersNotMarkedAsFriends)} = {dbUsersNotMarkedAsFriends.Count}");
                dbUsersNotMarkedAsFriends.ForEach(x =>
                {
                    x.Following = true;
                    x.FollowedDate = DateTime.Now;
                });
                LogDebug($"Marking {nameof(dbUsersNotMarkedAsFriends)} = {dbUsersNotMarkedAsFriends.Count} as friends");
                repo.Update(dbUsersNotMarkedAsFriends);
            }


        }


    }
}

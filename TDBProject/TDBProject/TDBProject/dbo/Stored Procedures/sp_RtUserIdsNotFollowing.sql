Create Procedure sp_RtUserIdsNotFollowing
AS

declare @myId bigint = dbo.MyUserId()
select UserId from 
(
select rt.UserId from VwMyMetrics vm join metrics rt on rt.RetweetId = vm.TweetId and rt.UserId!=@myId
union select rt.UserId from VwMyMetrics vm join metrics rt on rt.QuotedTweetId = vm.TweetId and rt.UserId!=@myId
) engIds
left join dbo.DbUsers u
on engIds.UserId=u.Id
where (Isnull(FollowsMe, 0)=0 and IsNull(following, 0)=0 and UnFollowedDate is null and UnFollowedMeDate is null)


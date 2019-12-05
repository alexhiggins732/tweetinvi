CREATE procedure [dbo].[CleanupExpired]
as 
declare @UserId bigint=  dbo.MyUserId() 
delete from Metrics where 
UserId!=@UserId and TweetId in 
(select TweetId from VwTopDeadEngagements where CreatedAt < DATEADD(hour, -6, getdate()))

select @@ROWCOUNT as DelCount
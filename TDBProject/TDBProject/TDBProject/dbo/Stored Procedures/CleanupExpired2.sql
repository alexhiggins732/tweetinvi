CREATE Procedure [dbo].[CleanupExpired2]
As 



declare @UserId bigint = dbo.MyUserId()
declare @expiredDate datetime= DateAdd(hour, -4, getdate())


select id into #deleteIds
from Metrics where UserId!=@userId and CreatedAt <@expiredDate

delete Metrics
from Metrics join #deleteIds d on Metrics.Id= d.id
select @@ROWCOUNT as deletedCount

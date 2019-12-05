
CREATE view [dbo].[VwTopDeadEngagements]
As


select top 100000 *, (cast(Engagements as float)/elapsed) * 60 as EPM  from 
(
	select TweetId, UserId,
	 Engagements= (RtCount +ReplyCount+ QuoteCount),
	 RtCount, ReplyCount, QuoteCount
	,Url
	, CreatedAt
	,elapsed

	from 
		(
			select t.TweetId, t.UserId,
			(select count(0) from Metrics where RetweetId=t.TweetId) as RtCount,
			(select count(0) from Metrics where ReplyToTweetId=t.TweetId) as ReplyCount,
			(select count(0) from Metrics where QuotedTweetId=t.TweetId) as QuoteCount,
			t.Url,
			t.CreatedAt,
			elapsed = DateDiff(SECOND, createdAt,getDate()) 
			from Metrics t where t.UserId!=139283160
		) counts
	) b

order by epm asc, CreatedAt asc



CREATE View [dbo].[VwMyEngagements] as

select top 100 percent *, (cast(Engagements as float)/elapsed) * 60 as EPM  from 
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
			(select count(0) from Metrics with (nolock) where RetweetId=t.TweetId) as RtCount,
			(select count(0) from Metrics with (nolock) where ReplyToTweetId=t.TweetId) as ReplyCount,
			(select count(0) from Metrics with (nolock) where QuotedTweetId=t.TweetId) as QuoteCount,
			t.Url,
			t.CreatedAt,
			elapsed = DateDiff(SECOND, createdAt,getDate()) 
			from Metrics t with (nolock)
		) counts
	) b
where b.UserId=139283160
order by epm desc

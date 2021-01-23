


CREATE View [dbo].[VwEngagements] as

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
			(select count(0) from Metrics where RetweetId=t.TweetId) as RtCount,
			(select count(0) from Metrics where ReplyToTweetId=t.TweetId) as ReplyCount,
			(select count(0) from Metrics where QuotedTweetId=t.TweetId) as QuoteCount,
			t.Url,
			t.CreatedAt,
			elapsed = DateDiff(SECOND, createdAt,getDate()) 
			from Metrics t
			--where t.CreatedAt<DateAdd(minute,-5, getdate())
		) counts
	) b
where b.Engagements>0
order by epm desc

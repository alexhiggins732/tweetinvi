create View vwTweetsWithType
As 

--select distinct TweetType
--from(
select m.TweetId,
	TweetType = 
	case when IsNull(m.QuotedTweetId, 0) =0 then 0 else 1 end
	+case when IsNull(m.ReplyToTweetId, 0) =0 then 0 else 2 end
	+case when IsNull(m.RetweetId, 0) =0 then 0 else 4 end
from Metrics m
--) typed


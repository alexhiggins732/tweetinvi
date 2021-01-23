
CREATE View [dbo].[VwMyActivityIds]
As

		select TweetId from Metrics with (nolock) where UserId=139283160
		union select ReplyToTweetId from Metrics m with (nolock) where UserId=139283160 and m.ReplyToTweetId is not null
		union select m.QuotedTweetId from Metrics m with (nolock) where UserId=139283160 and m.QuotedTweetId is not null
		union select m.RetweetId from Metrics m  with (nolock) where UserId=139283160 and m.RetweetId is not null

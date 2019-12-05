-- =============================================
-- Author:		Alexander Higgins
-- Create date: 10/1/2019
-- Description:	
-- =============================================
CREATE FUNCTION [dbo].[fnTweetType] 
(
	-- Add the parameters for the function here
	@TweetId bigint
)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	DECLARE @Result int =(select top 1	case when IsNull(m.QuotedTweetId, 0) =0 then 0 else 1 end
			+case when IsNull(m.ReplyToTweetId, 0) =0 then 0 else 2 end
		+case when IsNull(m.RetweetId, 0) =0 then 0 else 4 end
		from Metrics m where m.TweetId=@TweetId)

	-- Return the result of the function
	RETURN @Result

END

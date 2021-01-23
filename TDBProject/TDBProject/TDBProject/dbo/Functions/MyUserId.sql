-- =============================================
-- Author:		Alexander Higgins
-- Create date: 10/2/2019
-- Description:	
-- =============================================
CREATE FUNCTION MyUserId 
(
	-- Add the parameters for the function here
	
)
RETURNS bigint
AS
BEGIN

	declare @MyUserId bigint = (select top 1 Id from DbUsers where IsMyUser= 1)
	-- Return the result of the function
	RETURN @MyUserId

END

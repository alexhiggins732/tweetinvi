CREATE Function CleanCongressName(@name varchar(50))
returns varchar(50)
AS
Begin
	declare @result varchar(50) = @name
	set @result=replace(@result, 'Representative', '')
	set @result=replace(@result, 'Congressman', '')
	set @result=replace(@result, 'Congresswoman', '')
	set @result=replace(@result, 'Senator', '')
	set @result=replace(@result, 'Sen.', '')
	set @result=replace(@result, 'U.S.', '')
	--set @result=replace(@result, 'US', '')
	set @result=replace(@result, 'Rep.', '')
	return rtrim(ltrim(@result))
End


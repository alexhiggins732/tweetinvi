CREATE TABLE [dbo].[DbTweets] (
    [Id]           BIGINT         NOT NULL,
    [UserId]       BIGINT         NOT NULL,
    [Text]         NVARCHAR (260) NULL,
    [CreatedAt]    DATETIME       NOT NULL,
    [ReplyToId]    BIGINT         NULL,
    [QuotedId]     BIGINT         NULL,
    [RetweetId]    BIGINT         NULL,
    [RetweetCount] INT            NOT NULL,
    [ReplyCount]   INT            NULL,
    [LikeCount]    INT            NOT NULL,
    [Favorited]    BIT            NOT NULL,
    [Retweeted]    BIT            NOT NULL,
    CONSTRAINT [PK_dbo.DbTweets] PRIMARY KEY CLUSTERED ([Id] ASC)
);


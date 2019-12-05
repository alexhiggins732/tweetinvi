CREATE TABLE [dbo].[Metrics] (
    [Id]             INT            IDENTITY (1, 1) NOT NULL,
    [UserId]         BIGINT         NOT NULL,
    [TweetId]        BIGINT         NOT NULL,
    [RetweetId]      BIGINT         NULL,
    [QuotedTweetId]  BIGINT         NULL,
    [Deleted]        BIT            NOT NULL,
    [CreatedAt]      DATETIME       NOT NULL,
    [ReplyToTweetId] BIGINT         NULL,
    [Url]            NVARCHAR (100) NOT NULL,
    CONSTRAINT [PK_dbo.Metrics] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Metrics_TweetId]
    ON [dbo].[Metrics]([TweetId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Metrics_RetweetId]
    ON [dbo].[Metrics]([RetweetId] ASC);


GO
ALTER INDEX [IX_Metrics_RetweetId]
    ON [dbo].[Metrics] DISABLE;


GO
CREATE NONCLUSTERED INDEX [IX_Metrics_QuotedTweetId]
    ON [dbo].[Metrics]([QuotedTweetId] ASC);


GO
ALTER INDEX [IX_Metrics_QuotedTweetId]
    ON [dbo].[Metrics] DISABLE;


GO
CREATE NONCLUSTERED INDEX [IX_Metrics_ReplyToTweetId]
    ON [dbo].[Metrics]([ReplyToTweetId] ASC);


GO
ALTER INDEX [IX_Metrics_ReplyToTweetId]
    ON [dbo].[Metrics] DISABLE;


GO
CREATE NONCLUSTERED INDEX [IX_Metrics_UserId]
    ON [dbo].[Metrics]([UserId] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Metrics_UserId_CreatedAt]
    ON [dbo].[Metrics]([UserId] ASC, [CreatedAt] ASC)
    INCLUDE([Id]);


CREATE TABLE [dbo].[DbVideoDetails] (
    [Id]                     BIGINT          IDENTITY (1, 1) NOT NULL,
    [DurationInMilliseconds] BIGINT          NOT NULL,
    [Bitrate]                INT             NOT NULL,
    [ContentType]            NVARCHAR (100)  NULL,
    [URL]                    NVARCHAR (1000) NULL,
    [TweetMediaId]           BIGINT          NOT NULL,
    CONSTRAINT [PK_dbo.DbVideoDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_dbo.DbVideoDetails_dbo.DbTweetMedias_TweetMediaId] FOREIGN KEY ([TweetMediaId]) REFERENCES [dbo].[DbTweetMedias] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_TweetMediaId]
    ON [dbo].[DbVideoDetails]([TweetMediaId] ASC);


CREATE TABLE [dbo].[DbTweetDbTweetMedias] (
    [DbTweetMedia_Id] BIGINT NOT NULL,
    [DbTweet_Id]      BIGINT NOT NULL,
    CONSTRAINT [PK_dbo.DbTweetDbTweetMedias] PRIMARY KEY CLUSTERED ([DbTweet_Id] ASC, [DbTweetMedia_Id] ASC),
    CONSTRAINT [FK_dbo.DbTweetMediaDbTweets_dbo.DbTweetMedias_DbTweetMedia_Id] FOREIGN KEY ([DbTweetMedia_Id]) REFERENCES [dbo].[DbTweetMedias] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.DbTweetMediaDbTweets_dbo.DbTweets_DbTweet_Id] FOREIGN KEY ([DbTweet_Id]) REFERENCES [dbo].[DbTweets] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_DbTweetMedia_Id]
    ON [dbo].[DbTweetDbTweetMedias]([DbTweetMedia_Id] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_DbTweet_Id]
    ON [dbo].[DbTweetDbTweetMedias]([DbTweet_Id] ASC);


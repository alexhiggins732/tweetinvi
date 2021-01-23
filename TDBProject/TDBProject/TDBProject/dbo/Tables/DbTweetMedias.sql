CREATE TABLE [dbo].[DbTweetMedias] (
    [Id]            BIGINT          NOT NULL,
    [DisplayURL]    NVARCHAR (1000) NULL,
    [ExpandedURL]   NVARCHAR (1000) NULL,
    [MediaURL]      NVARCHAR (1000) NULL,
    [MediaURLHttps] NVARCHAR (1000) NULL,
    [MediaType]     NVARCHAR (100)  NULL,
    CONSTRAINT [PK_dbo.DbTweetMedias] PRIMARY KEY CLUSTERED ([Id] ASC)
);


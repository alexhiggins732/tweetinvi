CREATE TABLE [dbo].[DbLists] (
    [Id]              BIGINT        NOT NULL,
    [Name]            VARCHAR (100) NOT NULL,
    [FullName]        VARCHAR (250) NULL,
    [DbUserId]        BIGINT        NOT NULL,
    [CreatedAt]       DATETIME      NOT NULL,
    [Uri]             VARCHAR (500) NULL,
    [Description]     VARCHAR (500) NOT NULL,
    [Following]       BIT           NOT NULL,
    [Public]          BIT           NOT NULL,
    [MemberCount]     INT           NOT NULL,
    [SubscriberCount] INT           NOT NULL,
    CONSTRAINT [DbList_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [List_DbUserId] FOREIGN KEY ([DbUserId]) REFERENCES [dbo].[DbUsers] ([Id])
);


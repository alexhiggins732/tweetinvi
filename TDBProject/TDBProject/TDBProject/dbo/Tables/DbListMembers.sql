CREATE TABLE [dbo].[DbListMembers] (
    [Id]       INT    IDENTITY (1, 1) NOT NULL,
    [DbListId] BIGINT NOT NULL,
    [DbUserId] BIGINT NOT NULL,
    CONSTRAINT [ListMembers_PKId] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [ListMembers_LDbistsId] FOREIGN KEY ([DbListId]) REFERENCES [dbo].[DbLists] ([Id]),
    CONSTRAINT [ListMembers_UserId] FOREIGN KEY ([DbUserId]) REFERENCES [dbo].[DbUsers] ([Id])
);


CREATE TABLE [dbo].[SenateMembers] (
    [member_full]         VARCHAR (MAX) NULL,
    [last_name]           VARCHAR (MAX) NULL,
    [first_name]          VARCHAR (MAX) NULL,
    [party]               VARCHAR (MAX) NULL,
    [state]               VARCHAR (MAX) NULL,
    [address]             VARCHAR (MAX) NULL,
    [phone]               VARCHAR (MAX) NULL,
    [email]               VARCHAR (MAX) NULL,
    [website]             VARCHAR (MAX) NULL,
    [class]               VARCHAR (MAX) NULL,
    [bioguide_id]         VARCHAR (MAX) NULL,
    [leadership_position] VARCHAR (MAX) NULL,
    [AccountId]           BIGINT        NULL,
    [Id]                  BIGINT        IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [SenateMembers_PKId] PRIMARY KEY CLUSTERED ([Id] ASC)
);


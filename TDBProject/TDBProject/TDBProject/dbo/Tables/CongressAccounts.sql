CREATE TABLE [dbo].[CongressAccounts] (
    [Id]          BIGINT         NOT NULL,
    [ScreenName]  NVARCHAR (100) NULL,
    [Name]        NVARCHAR (100) NULL,
    [Description] NVARCHAR (260) NULL,
    [RepType]     VARCHAR (14)   NOT NULL,
    [Party]       VARCHAR (20)   NULL,
    [FullName]    VARCHAR (100)  NULL,
    [LastName]    VARCHAR (50)   NULL,
    [FirstName]   VARCHAR (50)   NULL
);


CREATE TABLE [dbo].[Influences] (
    [InfluencerId]        BIGINT NOT NULL,
    [InfluencedId]        BIGINT NOT NULL,
    [OriginalCount]       INT    CONSTRAINT [DF_Influences_OriginalCount] DEFAULT ((0)) NOT NULL,
    [QuoteCount]          INT    CONSTRAINT [DF_Influences_QuoteCount] DEFAULT ((0)) NOT NULL,
    [ReplyCount]          INT    CONSTRAINT [DF_Influences_ReplyCount] DEFAULT ((0)) NOT NULL,
    [ReplyWithQuoteCount] INT    CONSTRAINT [DF_Influences_ReplyWithQuoteCount] DEFAULT ((0)) NOT NULL,
    [RTCount]             INT    CONSTRAINT [DF_Influences_RTCount] DEFAULT ((0)) NOT NULL,
    [RTWithQuoteCount]    INT    CONSTRAINT [DF_Influences_RTWithQuoteCount] DEFAULT ((0)) NOT NULL,
    [Total]               AS     ((((([OriginalCount]+[QuoteCount])+[ReplyCount])+[ReplyWithQuoteCount])+[RTCount])+[RTWithQuoteCount]) PERSISTED,
    CONSTRAINT [PK_Influences] PRIMARY KEY CLUSTERED ([InfluencerId] ASC, [InfluencedId] ASC)
);


﻿CREATE TABLE [dbo].[HouseMembers] (
    [statedistrict]        VARCHAR (MAX) NULL,
    [namelist]             VARCHAR (MAX) NULL,
    [bioguideID]           VARCHAR (MAX) NULL,
    [lastname]             VARCHAR (MAX) NULL,
    [firstname]            VARCHAR (MAX) NULL,
    [middlename]           VARCHAR (MAX) NULL,
    [sortname]             VARCHAR (MAX) NULL,
    [suffix]               VARCHAR (MAX) NULL,
    [courtesy]             VARCHAR (MAX) NULL,
    [priorcongress]        VARCHAR (MAX) NULL,
    [officialname]         VARCHAR (MAX) NULL,
    [formalname]           VARCHAR (MAX) NULL,
    [party]                VARCHAR (MAX) NULL,
    [caucus]               VARCHAR (MAX) NULL,
    [statefullname]        VARCHAR (MAX) NULL,
    [postalcode]           VARCHAR (MAX) NULL,
    [district]             VARCHAR (MAX) NULL,
    [townname]             VARCHAR (MAX) NULL,
    [officebuilding]       VARCHAR (MAX) NULL,
    [officeroom]           VARCHAR (MAX) NULL,
    [officezip]            VARCHAR (MAX) NULL,
    [officezipsuffix]      VARCHAR (MAX) NULL,
    [phone]                VARCHAR (MAX) NULL,
    [electeddate]          VARCHAR (MAX) NULL,
    [sworndate]            VARCHAR (MAX) NULL,
    [footnoteref]          VARCHAR (MAX) NULL,
    [footnoterefSpecified] VARCHAR (MAX) NULL,
    [footnote]             VARCHAR (MAX) NULL,
    [AccountId]            BIGINT        NULL,
    [Id]                   BIGINT        IDENTITY (1, 1) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


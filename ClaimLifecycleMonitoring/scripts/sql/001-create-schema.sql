-- =============================================================================
-- ClaimLifecycleMonitoring - Schema creation script
-- Target: Microsoft SQL Server
-- =============================================================================

IF SCHEMA_ID(N'monitoring') IS NULL
    EXEC(N'CREATE SCHEMA [monitoring]');
GO

-- ----------------------------------------------------------------------------
-- Claims (aggregate root)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'[monitoring].[Claims]', N'U') IS NULL
BEGIN
    CREATE TABLE [monitoring].[Claims]
    (
        [Id]                          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Claims] PRIMARY KEY,
        [ClaimNumber]                 NVARCHAR(64)     NOT NULL,
        [CustomerCode]                NVARCHAR(64)     NOT NULL,
        [CustomerName]                NVARCHAR(256)    NOT NULL,
        [AccountCode]                 NVARCHAR(64)     NOT NULL,
        [AccountName]                 NVARCHAR(256)    NOT NULL,
        [PatientId]                   NVARCHAR(64)     NOT NULL,
        [PatientName]                 NVARCHAR(256)    NOT NULL,
        [ProviderId]                  NVARCHAR(64)     NOT NULL,
        [ProviderName]                NVARCHAR(256)    NOT NULL,
        [CurrentStage]                INT              NOT NULL,
        [CurrentStatus]               INT              NOT NULL,
        [LastSuccessfulStage]         INT              NULL,
        [NextExpectedStage]           INT              NULL,
        [StageEnteredUtc]             DATETIME2(3)     NOT NULL,
        [SubmissionDateUtc]           DATETIME2(3)     NULL,
        [HasFailure]                  BIT              NOT NULL CONSTRAINT [DF_Claims_HasFailure] DEFAULT (0),
        [FailureCategory]             INT              NOT NULL CONSTRAINT [DF_Claims_FailureCategory] DEFAULT (0),
        [ExceptionMessage]            NVARCHAR(4000)   NULL,
        [RetryAvailable]              BIT              NOT NULL CONSTRAINT [DF_Claims_RetryAvailable] DEFAULT (0),
        [RetryCount]                  INT              NOT NULL CONSTRAINT [DF_Claims_RetryCount] DEFAULT (0),
        [ManualInterventionRequired]  BIT              NOT NULL CONSTRAINT [DF_Claims_ManualIntervention] DEFAULT (0),
        [BilledAmount]                DECIMAL(18, 2)   NOT NULL CONSTRAINT [DF_Claims_BilledAmount] DEFAULT (0),
        [PaidAmount]                  DECIMAL(18, 2)   NOT NULL CONSTRAINT [DF_Claims_PaidAmount] DEFAULT (0),
        [CurrentStageSlaHours]        INT              NOT NULL CONSTRAINT [DF_Claims_CurrentStageSla] DEFAULT (24),
        [CreatedUtc]                  DATETIME2(3)     NOT NULL,
        [ModifiedUtc]                 DATETIME2(3)     NULL,
        [RowVersion]                  ROWVERSION       NOT NULL
    );

    CREATE UNIQUE INDEX [UX_Claims_ClaimNumber]
        ON [monitoring].[Claims] ([ClaimNumber]);
    CREATE INDEX [IX_Claims_CustomerAccount]
        ON [monitoring].[Claims] ([CustomerCode], [AccountCode]);
    CREATE INDEX [IX_Claims_CurrentStage]
        ON [monitoring].[Claims] ([CurrentStage]);
    CREATE INDEX [IX_Claims_CurrentStatus]
        ON [monitoring].[Claims] ([CurrentStatus]);
    CREATE INDEX [IX_Claims_ManualIntervention]
        ON [monitoring].[Claims] ([ManualInterventionRequired]);
END
GO

-- ----------------------------------------------------------------------------
-- Claim lifecycle events (history)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'[monitoring].[ClaimLifecycleEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [monitoring].[ClaimLifecycleEvents]
    (
        [Id]           UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ClaimLifecycleEvents] PRIMARY KEY,
        [ClaimId]      UNIQUEIDENTIFIER NOT NULL,
        [Stage]        INT              NOT NULL,
        [Outcome]      INT              NOT NULL,
        [Message]      NVARCHAR(2000)   NOT NULL,
        [ExceptionMessage] NVARCHAR(4000) NULL,
        [OccurredUtc]  DATETIME2(3)     NOT NULL,
        [CreatedUtc]   DATETIME2(3)     NOT NULL,
        [ModifiedUtc]  DATETIME2(3)     NULL,
        [RowVersion]   ROWVERSION       NOT NULL,
        CONSTRAINT [FK_ClaimLifecycleEvents_Claims]
            FOREIGN KEY ([ClaimId]) REFERENCES [monitoring].[Claims] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ClaimLifecycleEvents_ClaimId]
        ON [monitoring].[ClaimLifecycleEvents] ([ClaimId], [OccurredUtc]);
END
GO

-- ----------------------------------------------------------------------------
-- Claim alerts
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'[monitoring].[ClaimAlerts]', N'U') IS NULL
BEGIN
    CREATE TABLE [monitoring].[ClaimAlerts]
    (
        [Id]             UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ClaimAlerts] PRIMARY KEY,
        [ClaimId]        UNIQUEIDENTIFIER NOT NULL,
        [RuleId]         NVARCHAR(128)    NOT NULL,
        [Category]       INT              NOT NULL,
        [Severity]       INT              NOT NULL,
        [Message]        NVARCHAR(2000)   NOT NULL,
        [StageAtAlert]   INT              NOT NULL,
        [RaisedUtc]      DATETIME2(3)     NOT NULL,
        [Acknowledged]   BIT              NOT NULL CONSTRAINT [DF_ClaimAlerts_Ack] DEFAULT (0),
        [AcknowledgedBy] NVARCHAR(256)    NULL,
        [AcknowledgedUtc] DATETIME2(3)    NULL,
        [CreatedUtc]     DATETIME2(3)     NOT NULL,
        [ModifiedUtc]    DATETIME2(3)     NULL,
        [RowVersion]     ROWVERSION       NOT NULL,
        CONSTRAINT [FK_ClaimAlerts_Claims]
            FOREIGN KEY ([ClaimId]) REFERENCES [monitoring].[Claims] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ClaimAlerts_ClaimId] ON [monitoring].[ClaimAlerts] ([ClaimId]);
    CREATE INDEX [IX_ClaimAlerts_Severity] ON [monitoring].[ClaimAlerts] ([Severity], [Acknowledged]);
END
GO

-- ----------------------------------------------------------------------------
-- Stage SLA configuration
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'[monitoring].[StageSlaConfigurations]', N'U') IS NULL
BEGIN
    CREATE TABLE [monitoring].[StageSlaConfigurations]
    (
        [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_StageSlaConfigurations] PRIMARY KEY,
        [Stage]       INT              NOT NULL,
        [SlaHours]    INT              NOT NULL,
        [Description] NVARCHAR(512)    NULL,
        [CreatedUtc]  DATETIME2(3)     NOT NULL,
        [ModifiedUtc] DATETIME2(3)     NULL,
        [RowVersion]  ROWVERSION       NOT NULL
    );

    CREATE UNIQUE INDEX [UX_StageSlaConfigurations_Stage]
        ON [monitoring].[StageSlaConfigurations] ([Stage]);
END
GO

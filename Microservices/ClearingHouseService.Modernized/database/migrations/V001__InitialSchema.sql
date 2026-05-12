-- ============================================================
-- ClearingHouse Modernized Microservice - Enterprise Database Schema
-- Version: 1.0.0
-- Supports: High concurrency, massive insert throughput, audit traceability
-- ============================================================

-- ============================================================
-- SCHEMA: File Ingestion
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [ingestion];
GO

CREATE TABLE [ingestion].[InboundFiles] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [FileName] NVARCHAR(500) NOT NULL,
    [SourcePath] NVARCHAR(1000) NOT NULL,
    [ClearinghouseId] NVARCHAR(100) NOT NULL,
    [FileSizeBytes] BIGINT NOT NULL DEFAULT 0,
    [ContentHash] NVARCHAR(128) NULL,
    [BlobUri] NVARCHAR(2000) NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [DetectedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [DownloadedAt] DATETIME2(7) NULL,
    [UploadedToBlobAt] DATETIME2(7) NULL,
    [RetryCount] INT NOT NULL DEFAULT 0,
    [LastError] NVARCHAR(4000) NULL,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    [CreatedBy] NVARCHAR(200) NULL,
    [UpdatedBy] NVARCHAR(200) NULL,
    CONSTRAINT [PK_InboundFiles] PRIMARY KEY CLUSTERED ([Id])
) WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_InboundFiles_ClearinghouseId_Status]
    ON [ingestion].[InboundFiles] ([ClearinghouseId], [Status])
    INCLUDE ([FileName], [CorrelationId], [DetectedAt]);
GO

CREATE NONCLUSTERED INDEX [IX_InboundFiles_CorrelationId]
    ON [ingestion].[InboundFiles] ([CorrelationId])
    INCLUDE ([ClearinghouseId], [Status], [FileName]);
GO

CREATE NONCLUSTERED INDEX [IX_InboundFiles_ContentHash]
    ON [ingestion].[InboundFiles] ([ContentHash])
    WHERE [ContentHash] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_InboundFiles_Status_RetryCount]
    ON [ingestion].[InboundFiles] ([Status], [RetryCount])
    WHERE [Status] = 99 AND [RetryCount] < 3;
GO

CREATE TABLE [ingestion].[SftpConnections] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ClearinghouseId] NVARCHAR(100) NOT NULL,
    [Host] NVARCHAR(500) NOT NULL,
    [Port] INT NOT NULL DEFAULT 22,
    [Username] NVARCHAR(200) NOT NULL,
    [UploadDirectory] NVARCHAR(500) NULL,
    [DownloadDirectory] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [LastPolledAt] DATETIME2(7) NULL,
    [PollingIntervalSeconds] INT NOT NULL DEFAULT 300,
    [MaxRetryAttempts] INT NOT NULL DEFAULT 3,
    [ConnectionTimeoutSeconds] INT NOT NULL DEFAULT 30,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_SftpConnections] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_SftpConnections_ClearinghouseId] UNIQUE ([ClearinghouseId])
);
GO

-- ============================================================
-- SCHEMA: EDI Processing
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [edi];
GO

CREATE TABLE [edi].[EdiFiles] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [FileName] NVARCHAR(500) NOT NULL,
    [BlobUri] NVARCHAR(2000) NOT NULL,
    [FileType] INT NOT NULL,
    [ClearinghouseId] NVARCHAR(100) NOT NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [FileSizeBytes] BIGINT NOT NULL DEFAULT 0,
    [Status] INT NOT NULL DEFAULT 0,
    [TotalSegments] INT NOT NULL DEFAULT 0,
    [ProcessedSegments] INT NOT NULL DEFAULT 0,
    [ErrorCount] INT NOT NULL DEFAULT 0,
    [ProcessingStartedAt] DATETIME2(7) NULL,
    [ProcessingCompletedAt] DATETIME2(7) NULL,
    [LastError] NVARCHAR(4000) NULL,
    [RetryCount] INT NOT NULL DEFAULT 0,
    [BatchId] NVARCHAR(100) NULL,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_EdiFiles] PRIMARY KEY CLUSTERED ([Id])
) WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_EdiFiles_BatchId]
    ON [edi].[EdiFiles] ([BatchId])
    WHERE [BatchId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_EdiFiles_ClearinghouseId_Status]
    ON [edi].[EdiFiles] ([ClearinghouseId], [Status])
    INCLUDE ([FileName], [FileType], [CorrelationId]);
GO

CREATE NONCLUSTERED INDEX [IX_EdiFiles_Status_RetryCount]
    ON [edi].[EdiFiles] ([Status], [RetryCount])
    WHERE [Status] IN (3, 4);
GO

CREATE TABLE [edi].[EdiProcessingErrors] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [EdiFileId] UNIQUEIDENTIFIER NOT NULL,
    [Message] NVARCHAR(4000) NOT NULL,
    [OccurredAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_EdiProcessingErrors] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_EdiProcessingErrors_EdiFiles] FOREIGN KEY ([EdiFileId]) REFERENCES [edi].[EdiFiles]([Id])
);
GO

-- ============================================================
-- SCHEMA: Batch Orchestration
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [batch];
GO

CREATE TABLE [batch].[Batches] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [BatchName] NVARCHAR(500) NOT NULL,
    [ClearinghouseId] NVARCHAR(100) NOT NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [TotalItems] INT NOT NULL DEFAULT 0,
    [CompletedItems] INT NOT NULL DEFAULT 0,
    [FailedItems] INT NOT NULL DEFAULT 0,
    [MaxConcurrency] INT NOT NULL DEFAULT 10,
    [Priority] INT NOT NULL DEFAULT 1,
    [StartedAt] DATETIME2(7) NULL,
    [CompletedAt] DATETIME2(7) NULL,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_Batches] PRIMARY KEY CLUSTERED ([Id])
) WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_Batches_Status]
    ON [batch].[Batches] ([Status])
    INCLUDE ([ClearinghouseId], [Priority], [StartedAt]);
GO

CREATE TABLE [batch].[BatchItems] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [BatchId] UNIQUEIDENTIFIER NOT NULL,
    [FileId] UNIQUEIDENTIFIER NOT NULL,
    [FileName] NVARCHAR(500) NOT NULL,
    [BlobUri] NVARCHAR(2000) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [ProcessedAt] DATETIME2(7) NULL,
    [Error] NVARCHAR(4000) NULL,
    CONSTRAINT [PK_BatchItems] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_BatchItems_Batches] FOREIGN KEY ([BatchId]) REFERENCES [batch].[Batches]([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_BatchItems_BatchId_Status]
    ON [batch].[BatchItems] ([BatchId], [Status]);
GO

-- ============================================================
-- SCHEMA: Reconciliation
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [reconciliation];
GO

CREATE TABLE [reconciliation].[ClaimReconciliation] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [ClaimId] NVARCHAR(100) NOT NULL,
    [PatientControlNumber] NVARCHAR(100) NOT NULL,
    [PayerClaimId] NVARCHAR(100) NULL,
    [ClearinghouseId] NVARCHAR(100) NOT NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [SubmittedAmount] DECIMAL(18,2) NULL,
    [PaidAmount] DECIMAL(18,2) NULL,
    [AdjustmentAmount] DECIMAL(18,2) NULL,
    [AdjustmentReasonCode] NVARCHAR(50) NULL,
    [SubmittedAt] DATETIME2(7) NOT NULL,
    [AcknowledgedAt] DATETIME2(7) NULL,
    [PaidAt] DATETIME2(7) NULL,
    [ReconciledAt] DATETIME2(7) NULL,
    [SubmissionFileId] NVARCHAR(100) NULL,
    [ResponseFileId] NVARCHAR(100) NULL,
    [EraFileId] NVARCHAR(100) NULL,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_ClaimReconciliation] PRIMARY KEY CLUSTERED ([Id])
) WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_ClaimReconciliation_ClaimId]
    ON [reconciliation].[ClaimReconciliation] ([ClaimId])
    INCLUDE ([Status], [SubmittedAmount], [PaidAmount]);
GO

CREATE NONCLUSTERED INDEX [IX_ClaimReconciliation_PatientControlNumber]
    ON [reconciliation].[ClaimReconciliation] ([PatientControlNumber])
    INCLUDE ([ClaimId], [Status], [ClearinghouseId]);
GO

CREATE NONCLUSTERED INDEX [IX_ClaimReconciliation_Status_ClearinghouseId]
    ON [reconciliation].[ClaimReconciliation] ([Status], [ClearinghouseId])
    INCLUDE ([ClaimId], [SubmittedAt]);
GO

-- ============================================================
-- SCHEMA: File Tracking
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [tracking];
GO

CREATE TABLE [tracking].[FileLifecycles] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [FileName] NVARCHAR(500) NOT NULL,
    [ClearinghouseId] NVARCHAR(100) NOT NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [Direction] INT NOT NULL,
    [EdiType] NVARCHAR(50) NULL,
    [FileSizeBytes] BIGINT NOT NULL DEFAULT 0,
    [BlobUri] NVARCHAR(2000) NULL,
    [CurrentStatus] INT NOT NULL DEFAULT 0,
    [ArchivedAt] DATETIME2(7) NULL,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_FileLifecycles] PRIMARY KEY CLUSTERED ([Id])
) WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_FileLifecycles_CorrelationId]
    ON [tracking].[FileLifecycles] ([CorrelationId]);
GO

CREATE NONCLUSTERED INDEX [IX_FileLifecycles_ClearinghouseId_Status]
    ON [tracking].[FileLifecycles] ([ClearinghouseId], [CurrentStatus])
    INCLUDE ([FileName], [Direction], [CreatedAt]);
GO

CREATE TABLE [tracking].[FileEvents] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [FileLifecycleId] UNIQUEIDENTIFIER NOT NULL,
    [Status] INT NOT NULL,
    [Description] NVARCHAR(1000) NOT NULL,
    [Details] NVARCHAR(4000) NULL,
    [OccurredAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FileEvents] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_FileEvents_FileLifecycles] FOREIGN KEY ([FileLifecycleId]) REFERENCES [tracking].[FileLifecycles]([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_FileEvents_FileLifecycleId]
    ON [tracking].[FileEvents] ([FileLifecycleId])
    INCLUDE ([Status], [OccurredAt]);
GO

-- ============================================================
-- SCHEMA: Notifications
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [notification];
GO

CREATE TABLE [notification].[OperationalAlerts] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Title] NVARCHAR(500) NOT NULL,
    [Message] NVARCHAR(4000) NOT NULL,
    [Severity] INT NOT NULL,
    [Category] INT NOT NULL,
    [SourceService] NVARCHAR(200) NOT NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [IsAcknowledged] BIT NOT NULL DEFAULT 0,
    [AcknowledgedAt] DATETIME2(7) NULL,
    [AcknowledgedBy] NVARCHAR(200) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [Version] BIGINT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_OperationalAlerts] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_OperationalAlerts_Severity_IsAcknowledged]
    ON [notification].[OperationalAlerts] ([Severity], [IsAcknowledged])
    INCLUDE ([Title], [Category], [CreatedAt]);
GO

-- ============================================================
-- SCHEMA: Audit / Event History
-- ============================================================
CREATE SCHEMA IF NOT EXISTS [audit];
GO

CREATE TABLE [audit].[EventHistory] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [EventId] UNIQUEIDENTIFIER NOT NULL,
    [EventType] NVARCHAR(500) NOT NULL,
    [AggregateId] NVARCHAR(100) NOT NULL,
    [AggregateType] NVARCHAR(200) NOT NULL,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [Payload] NVARCHAR(MAX) NOT NULL,
    [OccurredAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [PublishedAt] DATETIME2(7) NULL,
    [ProcessedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_EventHistory] PRIMARY KEY CLUSTERED ([Id])
) WITH (DATA_COMPRESSION = PAGE);
GO

CREATE NONCLUSTERED INDEX [IX_EventHistory_AggregateId]
    ON [audit].[EventHistory] ([AggregateId], [AggregateType])
    INCLUDE ([EventType], [OccurredAt]);
GO

CREATE NONCLUSTERED INDEX [IX_EventHistory_CorrelationId]
    ON [audit].[EventHistory] ([CorrelationId])
    INCLUDE ([EventType], [OccurredAt]);
GO

CREATE NONCLUSTERED INDEX [IX_EventHistory_EventType_OccurredAt]
    ON [audit].[EventHistory] ([EventType], [OccurredAt]);
GO

-- ============================================================
-- Partitioning Strategy for High-Volume Tables
-- ============================================================
-- NOTE: For production, implement date-based partition functions on:
-- - [audit].[EventHistory] by OccurredAt (monthly partitions)
-- - [tracking].[FileEvents] by OccurredAt (monthly partitions)
-- - [ingestion].[InboundFiles] by DetectedAt (monthly partitions)
-- This enables efficient partition elimination and sliding window archival.

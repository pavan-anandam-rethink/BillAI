-- 002-outbox-table.sql
-- Creates the durable outbox table for BillingService transactional event publishing.
-- Pairs with the outbox publisher worker and IOutboxPoller.
-- Rollback: 002-outbox-table.rollback.sql

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing')
BEGIN
    EXEC ('CREATE SCHEMA billing');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'billing' AND t.name = 'BillingOutbox'
)
BEGIN
    CREATE TABLE billing.BillingOutbox (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        EventType       NVARCHAR(256)       NOT NULL,
        Payload         NVARCHAR(MAX)       NOT NULL,
        CorrelationId   NVARCHAR(128)       NULL,
        OccurredOnUtc   DATETIMEOFFSET      NOT NULL,
        ProcessedOnUtc  DATETIMEOFFSET      NULL,
        ProcessingError NVARCHAR(MAX)       NULL,
        AttemptCount    INT                 NOT NULL DEFAULT 0,
        CreatedAtUtc    DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_BillingOutbox PRIMARY KEY NONCLUSTERED (Id)
    );

    -- Clustered index on processing order for efficient polling of unprocessed rows.
    CREATE CLUSTERED INDEX CIX_BillingOutbox_OccurredOnUtc
        ON billing.BillingOutbox (OccurredOnUtc ASC);

    -- Filtered index for the outbox poller hot path: unprocessed rows only.
    CREATE NONCLUSTERED INDEX IX_BillingOutbox_Unprocessed
        ON billing.BillingOutbox (OccurredOnUtc ASC)
        WHERE ProcessedOnUtc IS NULL;

    PRINT 'Created billing.BillingOutbox table and indexes.';
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'billing' AND t.name = 'BlobMetadata'
)
BEGIN
    CREATE TABLE billing.BlobMetadata (
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
        CorrelationId   NVARCHAR(128)       NOT NULL,
        BlobName        NVARCHAR(1024)      NOT NULL,
        ContainerName   NVARCHAR(256)       NOT NULL,
        Tags            NVARCHAR(MAX)       NULL,       -- JSON key-value pairs; never file paths
        CreatedOnUtc    DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_BlobMetadata PRIMARY KEY (Id)
    );

    CREATE NONCLUSTERED INDEX IX_BlobMetadata_CorrelationId
        ON billing.BlobMetadata (CorrelationId)
        INCLUDE (BlobName, ContainerName, CreatedOnUtc);

    PRINT 'Created billing.BlobMetadata table and indexes.';
END;
GO

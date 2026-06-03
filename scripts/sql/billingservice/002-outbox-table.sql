-- ============================================================================
-- 002-outbox-table.sql
-- Creates the BillingOutbox table required by the transactional outbox pattern.
-- SAFE TO RUN ON LIVE DATABASE: uses IF NOT EXISTS guards.
-- Run this script BEFORE enabling BillingService:Modernization:EnableOutboxPublisher.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'billing')
BEGIN
    EXEC('CREATE SCHEMA billing');
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM   sys.tables t
    JOIN   sys.schemas s ON s.schema_id = t.schema_id
    WHERE  s.name = N'billing' AND t.name = N'BillingOutbox'
)
BEGIN
    CREATE TABLE billing.BillingOutbox
    (
        Id              UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_BillingOutbox PRIMARY KEY NONCLUSTERED,
        EventType       NVARCHAR(512)       NOT NULL,
        Payload         NVARCHAR(MAX)       NOT NULL,
        CorrelationId   NVARCHAR(128)       NULL,
        OccurredOnUtc   DATETIMEOFFSET(7)   NOT NULL,
        ProcessedOnUtc  DATETIMEOFFSET(7)   NULL,
        ProcessingError NVARCHAR(2000)      NULL,
        AttemptCount    INT                 NOT NULL CONSTRAINT DF_BillingOutbox_AttemptCount DEFAULT 0
    );

    -- Clustered index on OccurredOnUtc for efficient poll-by-time ordering.
    CREATE CLUSTERED INDEX CIX_BillingOutbox_OccurredOnUtc
        ON billing.BillingOutbox (OccurredOnUtc ASC);

    -- Covering index for the polling query: unprocessed rows ordered by time.
    CREATE NONCLUSTERED INDEX IX_BillingOutbox_Pending
        ON billing.BillingOutbox (ProcessedOnUtc ASC, AttemptCount ASC, OccurredOnUtc ASC)
        INCLUDE (Id, EventType, CorrelationId)
        WHERE ProcessedOnUtc IS NULL;

    PRINT 'Created table billing.BillingOutbox with indexes.';
END
ELSE
BEGIN
    PRINT 'Table billing.BillingOutbox already exists — skipped.';
END
GO

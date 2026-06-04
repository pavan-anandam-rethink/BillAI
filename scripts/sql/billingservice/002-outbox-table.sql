/*
BillingService durable outbox table.

Creates the billing.BillingOutbox table used by the outbox publisher pattern.
Messages are written transactionally alongside their owning business entity
and relayed to Azure Service Bus by OutboxPublisherWorker.

Safe to run multiple times (idempotent).
*/

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing')
BEGIN
    EXEC('CREATE SCHEMA billing AUTHORIZATION dbo');
END;

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'billing.BillingOutbox') AND type = 'U')
BEGIN
    CREATE TABLE billing.BillingOutbox (
        Id              UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_BillingOutbox PRIMARY KEY NONCLUSTERED,
        EventType       NVARCHAR(256)       NOT NULL,
        Payload         NVARCHAR(MAX)       NOT NULL,
        CorrelationId   NVARCHAR(128)       NULL,
        OccurredOnUtc   DATETIMEOFFSET(7)   NOT NULL,
        ProcessedOnUtc  DATETIMEOFFSET(7)   NULL,
        ProcessingError NVARCHAR(2000)      NULL,
        AttemptCount    INT                 NOT NULL CONSTRAINT DF_BillingOutbox_AttemptCount DEFAULT 0
    );

    -- Index for the poll query (unprocessed, ordered by OccurredOnUtc)
    CREATE CLUSTERED INDEX CIX_BillingOutbox_OccurredOnUtc
        ON billing.BillingOutbox (OccurredOnUtc ASC);

    CREATE NONCLUSTERED INDEX IX_BillingOutbox_Unprocessed
        ON billing.BillingOutbox (ProcessedOnUtc, AttemptCount)
        INCLUDE (Id, EventType, CorrelationId, OccurredOnUtc)
        WHERE ProcessedOnUtc IS NULL;
END;

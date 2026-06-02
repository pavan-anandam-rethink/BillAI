/*
BillingService durable outbox table.

Creates the BillingOutbox table used by the outbox publisher worker to
guarantee at-least-once delivery of integration events to Azure Service Bus.

Rules:
- All DDL statements are idempotent.
- This migration is additive and non-breaking: no existing tables are modified.
- The table stores lightweight event envelopes only; no blob file paths or
  physical document references are stored here.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.BillingOutbox')
      AND type = N'U'
)
BEGIN
    CREATE TABLE dbo.BillingOutbox
    (
        Id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        EventId          UNIQUEIDENTIFIER NOT NULL,
        EventType        NVARCHAR(256)    NOT NULL,
        AggregateType    NVARCHAR(256)    NOT NULL,
        AggregateId      NVARCHAR(512)    NOT NULL,
        CorrelationId    NVARCHAR(256)    NULL,
        Payload          NVARCHAR(MAX)    NOT NULL,
        SchemaVersion    INT              NOT NULL DEFAULT 1,
        CreatedOnUtc     DATETIMEOFFSET   NOT NULL DEFAULT SYSUTCDATETIME(),
        ProcessedOnUtc   DATETIMEOFFSET   NULL,
        Error            NVARCHAR(2048)   NULL,
        RetryCount       INT              NOT NULL DEFAULT 0,
        CONSTRAINT PK_BillingOutbox PRIMARY KEY CLUSTERED (Id)
    );

    CREATE NONCLUSTERED INDEX IX_BillingOutbox_Unprocessed
    ON dbo.BillingOutbox (ProcessedOnUtc, CreatedOnUtc)
    INCLUDE (Id, EventId, EventType, Payload, CorrelationId, RetryCount)
    WHERE ProcessedOnUtc IS NULL
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON);

    PRINT 'Created dbo.BillingOutbox and IX_BillingOutbox_Unprocessed';
END;
ELSE
BEGIN
    PRINT 'dbo.BillingOutbox already exists; skipping creation.';
END;

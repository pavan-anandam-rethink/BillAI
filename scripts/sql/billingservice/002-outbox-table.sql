/*
BillingService durable outbox table.

Idempotent: the table and index are only created when they do not already exist.
Rollback script: 002-outbox-table.rollback.sql
*/

IF NOT EXISTS (
    SELECT 1
    FROM   sys.tables
    WHERE  name = 'BillingOutbox'
      AND  SCHEMA_NAME(schema_id) = 'dbo'
)
BEGIN
    CREATE TABLE dbo.BillingOutbox
    (
        Id               UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_BillingOutbox PRIMARY KEY NONCLUSTERED DEFAULT (NEWID()),
        EventType        NVARCHAR(256)    NOT NULL,
        Payload          NVARCHAR(MAX)    NOT NULL,
        CorrelationId    NVARCHAR(128)    NULL,
        OccurredOnUtc    DATETIMEOFFSET   NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
        ProcessedOnUtc   DATETIMEOFFSET   NULL,
        ProcessingError  NVARCHAR(1024)   NULL,
        AttemptCount     INT              NOT NULL DEFAULT 0
    );
END;

/* Clustered index on the processing-order column so the outbox worker scans efficiently. */
IF NOT EXISTS (
    SELECT 1
    FROM   sys.indexes
    WHERE  name = 'CIX_BillingOutbox_OccurredOnUtc'
      AND  object_id = OBJECT_ID(N'dbo.BillingOutbox')
)
BEGIN
    CREATE CLUSTERED INDEX CIX_BillingOutbox_OccurredOnUtc
    ON dbo.BillingOutbox (OccurredOnUtc ASC);
END;

/* Covering index for the outbox worker query (unprocessed events ordered by age). */
IF NOT EXISTS (
    SELECT 1
    FROM   sys.indexes
    WHERE  name = 'IX_BillingOutbox_ProcessedOnUtc_OccurredOnUtc'
      AND  object_id = OBJECT_ID(N'dbo.BillingOutbox')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BillingOutbox_ProcessedOnUtc_OccurredOnUtc
    ON dbo.BillingOutbox (ProcessedOnUtc, OccurredOnUtc)
    INCLUDE (Id, EventType, Payload, CorrelationId, AttemptCount)
    WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
END;

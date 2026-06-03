/*
BillingService durable outbox table migration.

Creates the billing.OutboxMessages table used by OutboxPersistenceWriter (write path)
and SqlOutboxPoller (read path).

All statements are idempotent. Safe to run multiple times against the same database.
Roll back with: scripts/sql/billingservice/002-outbox-table.rollback.sql
*/

-- Ensure the billing schema exists
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'billing')
BEGIN
    EXEC(N'CREATE SCHEMA billing AUTHORIZATION dbo');
END;

-- Create the outbox table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'billing.OutboxMessages') AND type = N'U')
BEGIN
    CREATE TABLE billing.OutboxMessages
    (
        Id               UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_OutboxMessages PRIMARY KEY NONCLUSTERED,
        EventType        NVARCHAR(500)    NOT NULL,
        Payload          NVARCHAR(MAX)    NOT NULL,
        CorrelationId    NVARCHAR(128)    NULL,
        OccurredOnUtc    DATETIMEOFFSET   NOT NULL,
        ProcessedOnUtc   DATETIMEOFFSET   NULL,
        ProcessingError  NVARCHAR(2000)   NULL,
        AttemptCount     INT              NOT NULL CONSTRAINT DF_OutboxMessages_AttemptCount DEFAULT 0
    );
END;

-- Clustered index on OccurredOnUtc for efficient FIFO polling
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OutboxMessages_OccurredOnUtc' AND object_id = OBJECT_ID(N'billing.OutboxMessages'))
BEGIN
    CREATE CLUSTERED INDEX IX_OutboxMessages_OccurredOnUtc
    ON billing.OutboxMessages (OccurredOnUtc)
    WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
END;

-- Filtered index covering only unprocessed rows; used by the poller query
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OutboxMessages_Pending' AND object_id = OBJECT_ID(N'billing.OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_Pending
    ON billing.OutboxMessages (OccurredOnUtc, AttemptCount)
    INCLUDE (Id, EventType, Payload, CorrelationId, ProcessingError)
    WHERE ProcessedOnUtc IS NULL
    WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);
END;

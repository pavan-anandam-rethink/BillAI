/*
BillingService durable outbox table.

Creates the [billing].[OutboxMessages] table used by the transactional outbox pattern.
This table is written inside the same SQL transaction as the business event that triggered it,
guaranteeing at-least-once delivery to Azure Service Bus without distributed transactions.

Safe to run multiple times — all statements are idempotent.
*/

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing')
BEGIN
    EXEC('CREATE SCHEMA [billing]');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'billing' AND t.name = 'OutboxMessages'
)
BEGIN
    CREATE TABLE [billing].[OutboxMessages]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
        [EventType]       NVARCHAR(500)    NOT NULL,
        [Payload]         NVARCHAR(MAX)    NOT NULL,
        [CorrelationId]   NVARCHAR(128)    NULL,
        [OccurredOnUtc]   DATETIMEOFFSET   NOT NULL,
        [ProcessedOnUtc]  DATETIMEOFFSET   NULL,
        [ProcessingError] NVARCHAR(2000)   NULL,
        [AttemptCount]    INT              NOT NULL CONSTRAINT DF_OutboxMessages_AttemptCount DEFAULT 0
    );

    -- Optimise polling: fetch unprocessed rows ordered by occurrence time.
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_Pending
    ON [billing].[OutboxMessages] (OccurredOnUtc ASC)
    WHERE ProcessedOnUtc IS NULL
    WITH (ONLINE = ON, DATA_COMPRESSION = PAGE);

    -- Optimise correlation-ID look-ups.
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_CorrelationId
    ON [billing].[OutboxMessages] (CorrelationId)
    WHERE CorrelationId IS NOT NULL
    WITH (ONLINE = ON);
END;
GO

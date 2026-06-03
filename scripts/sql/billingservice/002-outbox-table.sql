-- BillingService Durable Outbox Table
-- Migration: 002-outbox-table.sql
-- Purpose: Stores integration events transactionally alongside billing operations.
--          Enables at-least-once delivery via the outbox pattern without distributed transactions.
-- Rollback: 002-outbox-table.rollback.sql

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'billing')
BEGIN
    EXEC(N'CREATE SCHEMA [billing]');
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'billing' AND t.name = N'OutboxMessages'
)
BEGIN
    CREATE TABLE [billing].[OutboxMessages]
    (
        [Id]              UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWSEQUENTIALID(),
        [EventType]       NVARCHAR(500)     NOT NULL,
        [Payload]         NVARCHAR(MAX)     NOT NULL,
        [CorrelationId]   NVARCHAR(200)     NULL,
        [OccurredOnUtc]   DATETIMEOFFSET    NOT NULL,
        [ProcessedOnUtc]  DATETIMEOFFSET    NULL,
        [ProcessingError] NVARCHAR(1000)    NULL,
        [AttemptCount]    INT               NOT NULL DEFAULT 0,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );

    -- Covering index for the outbox poller query:
    --   WHERE ProcessedOnUtc IS NULL AND AttemptCount < N ORDER BY OccurredOnUtc ASC
    CREATE NONCLUSTERED INDEX [IX_OutboxMessages_Unprocessed_OccurredOnUtc]
    ON [billing].[OutboxMessages] ([ProcessedOnUtc], [AttemptCount], [OccurredOnUtc])
    INCLUDE ([Id], [EventType], [CorrelationId]);

    -- Index to support correlation ID lookup for tracing and diagnostics.
    CREATE NONCLUSTERED INDEX [IX_OutboxMessages_CorrelationId]
    ON [billing].[OutboxMessages] ([CorrelationId])
    WHERE [CorrelationId] IS NOT NULL;
END;
GO

-- ============================================================================
-- Script: 002-outbox-table.sql
-- Purpose: Creates the BillingOutboxMessage table that backs the durable outbox
--          pattern implemented by OutboxPublisherWorker.
-- Safety: Fully additive — no existing tables, columns, or indexes are modified.
-- Rollback: 002-outbox-table.rollback.sql
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[BillingOutboxMessage]')
      AND type_desc = 'USER_TABLE'
)
BEGIN
    CREATE TABLE [dbo].[BillingOutboxMessage]
    (
        -- Stable identity; doubles as an idempotency key for event bus publishing.
        [Id]              UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_BillingOutboxMessage_Id DEFAULT NEWSEQUENTIALID(),

        -- The integration event type name (e.g. "billing.operation.completed").
        [EventType]       NVARCHAR(200)     NOT NULL,

        -- JSON-serialized integration event payload.
        [Payload]         NVARCHAR(MAX)     NOT NULL,

        -- Correlation ID propagated from the originating HTTP request.
        [CorrelationId]   NVARCHAR(100)     NULL,

        -- When the domain operation that raised this event completed (UTC).
        [OccurredOnUtc]   DATETIMEOFFSET(7) NOT NULL,

        -- Populated by OutboxPublisherWorker once the event is confirmed
        -- delivered to the Azure Service Bus topic.
        [ProcessedOnUtc]  DATETIMEOFFSET(7) NULL,

        -- Last error message from a failed publish attempt.
        [ProcessingError] NVARCHAR(2000)    NULL,

        -- Incremented on each publish attempt to support dead-letter detection.
        [AttemptCount]    INT               NOT NULL CONSTRAINT DF_BillingOutboxMessage_AttemptCount DEFAULT 0,

        CONSTRAINT PK_BillingOutboxMessage PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    WITH (DATA_COMPRESSION = ROW);

    -- Index for the poller query: fetch unprocessed events ordered by occurrence.
    CREATE NONCLUSTERED INDEX IX_BillingOutboxMessage_Unprocessed
        ON [dbo].[BillingOutboxMessage] ([ProcessedOnUtc] ASC, [OccurredOnUtc] ASC)
        WHERE [ProcessedOnUtc] IS NULL
        WITH (DATA_COMPRESSION = ROW, ONLINE = ON);

    -- Index to look up events by correlation ID for tracing and debugging.
    CREATE NONCLUSTERED INDEX IX_BillingOutboxMessage_CorrelationId
        ON [dbo].[BillingOutboxMessage] ([CorrelationId] ASC)
        WHERE [CorrelationId] IS NOT NULL
        WITH (DATA_COMPRESSION = ROW, ONLINE = ON);

    PRINT 'Created BillingOutboxMessage table and indexes.';
END
ELSE
BEGIN
    PRINT 'BillingOutboxMessage table already exists — skipping creation.';
END
GO

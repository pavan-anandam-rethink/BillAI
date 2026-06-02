/*
BillingService durable outbox table.

Creates the BillingOutboxMessages table used by the transactional outbox pattern.
The OutboxPublisherWorker reads pending rows and publishes them to Azure Service Bus.

Design:
  - Id is the EventId (Guid) from IntegrationEvent – guarantees idempotent insert.
  - Payload stores the full JSON-serialised integration event.
  - ProcessedOnUtc is NULL until the publisher confirms delivery.
  - The covering index on ProcessedOnUtc supports the worker query without full scans.

All statements are idempotent.
*/

IF NOT EXISTS (
    SELECT 1
    FROM   sys.tables t
    JOIN   sys.schemas s ON s.schema_id = t.schema_id
    WHERE  s.name = 'dbo' AND t.name = 'BillingOutboxMessages'
)
BEGIN
    CREATE TABLE dbo.BillingOutboxMessages
    (
        Id              UNIQUEIDENTIFIER    NOT NULL CONSTRAINT PK_BillingOutboxMessages PRIMARY KEY,
        EventType       NVARCHAR(256)       NOT NULL,
        AggregateType   NVARCHAR(128)       NOT NULL,
        AggregateId     NVARCHAR(256)       NOT NULL,
        CorrelationId   NVARCHAR(128)       NULL,
        Payload         NVARCHAR(MAX)       NOT NULL,
        CreatedOnUtc    DATETIME2(7)        NOT NULL CONSTRAINT DF_BillingOutboxMessages_CreatedOnUtc DEFAULT SYSUTCDATETIME(),
        ProcessedOnUtc  DATETIME2(7)        NULL
    );
END;

-- Covering index for the worker poll: unprocessed events ordered by creation time.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'IX_BillingOutboxMessages_Unprocessed'
    AND    object_id = OBJECT_ID(N'dbo.BillingOutboxMessages')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BillingOutboxMessages_Unprocessed
    ON dbo.BillingOutboxMessages (ProcessedOnUtc, CreatedOnUtc)
    INCLUDE (Id, EventType, AggregateType, AggregateId, CorrelationId, Payload)
    WHERE ProcessedOnUtc IS NULL
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON);
END;

-- Index to support retrieval by CorrelationId for distributed tracing.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name = 'IX_BillingOutboxMessages_CorrelationId'
    AND    object_id = OBJECT_ID(N'dbo.BillingOutboxMessages')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_BillingOutboxMessages_CorrelationId
    ON dbo.BillingOutboxMessages (CorrelationId)
    INCLUDE (Id, EventType, CreatedOnUtc, ProcessedOnUtc)
    WHERE CorrelationId IS NOT NULL
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON);
END;

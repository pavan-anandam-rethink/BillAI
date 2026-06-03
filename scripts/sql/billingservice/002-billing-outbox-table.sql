/*
BillingService durable outbox table migration.

Creates the BillingOutbox table used by OutboxPersistenceWriter and SqlOutboxPoller.
All statements are idempotent. Run before enabling EnableOutboxPublisher feature flag.

Columns:
  Id              - Integration event ID (same as IntegrationEvent.EventId).
  EventType       - Discriminator string (e.g. "billing.operation.completed").
  Payload         - JSON-serialized integration event body.
  CorrelationId   - X-Correlation-Id propagated from the originating HTTP request.
  OccurredOnUtc   - When the event occurred (UTC).
  ProcessedOnUtc  - Set by SqlOutboxPoller after successful Service Bus publish.
  ProcessingError - Last error message from a failed delivery attempt (max 2000 chars).
  AttemptCount    - Number of delivery attempts; rows with AttemptCount >= 5 are skipped.
*/

IF NOT EXISTS (
    SELECT 1 FROM sys.objects WHERE name = 'BillingOutbox' AND type = 'U'
)
BEGIN
    CREATE TABLE dbo.BillingOutbox
    (
        Id              UNIQUEIDENTIFIER NOT NULL,
        EventType       NVARCHAR(200)    NOT NULL,
        Payload         NVARCHAR(MAX)    NOT NULL,
        CorrelationId   NVARCHAR(100)    NULL,
        OccurredOnUtc   DATETIMEOFFSET   NOT NULL,
        ProcessedOnUtc  DATETIMEOFFSET   NULL,
        ProcessingError NVARCHAR(2000)   NULL,
        AttemptCount    INT              NOT NULL DEFAULT 0,

        CONSTRAINT PK_BillingOutbox PRIMARY KEY CLUSTERED (Id)
    );

    -- Covering index for the poller query (unprocessed, ordered by OccurredOnUtc).
    CREATE NONCLUSTERED INDEX IX_BillingOutbox_Unprocessed_OccurredOnUtc
    ON dbo.BillingOutbox (ProcessedOnUtc, AttemptCount, OccurredOnUtc)
    INCLUDE (Id, EventType, Payload, CorrelationId)
    WHERE ProcessedOnUtc IS NULL
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

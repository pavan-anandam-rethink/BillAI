-- BillingService Durable Outbox Table Rollback
-- Rollback: 002-outbox-table.rollback.sql
-- Reverses: 002-outbox-table.sql
-- WARNING: This destroys all unprocessed outbox messages. Run only after draining the
--          OutboxPublisherWorker and confirming no pending messages remain.

IF EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'billing' AND t.name = N'OutboxMessages'
)
BEGIN
    DROP TABLE [billing].[OutboxMessages];
END;
GO

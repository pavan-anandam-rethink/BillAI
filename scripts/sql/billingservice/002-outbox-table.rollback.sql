/*
Rollback for 002-outbox-table.sql

Drops the BillingOutboxMessages table and its indexes.
Run this before re-applying the forward migration if a rollback is required.

WARNING: This permanently removes all pending outbox events.
Drain and confirm the OutboxPublisherWorker has processed all rows before running.
*/

IF EXISTS (
    SELECT 1
    FROM   sys.tables t
    JOIN   sys.schemas s ON s.schema_id = t.schema_id
    WHERE  s.name = 'dbo' AND t.name = 'BillingOutboxMessages'
)
BEGIN
    DROP TABLE dbo.BillingOutboxMessages;
END;

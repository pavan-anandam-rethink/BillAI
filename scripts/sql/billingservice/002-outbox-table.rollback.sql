-- ============================================================================
-- 002-outbox-table.rollback.sql
-- Drops the BillingOutbox table created by 002-outbox-table.sql.
-- WARNING: This will destroy any unprocessed outbox rows. Drain the outbox
-- worker and verify ProcessedOnUtc IS NOT NULL for all rows before running.
-- ============================================================================

IF EXISTS (
    SELECT 1
    FROM   sys.tables t
    JOIN   sys.schemas s ON s.schema_id = t.schema_id
    WHERE  s.name = N'billing' AND t.name = N'BillingOutbox'
)
BEGIN
    DROP TABLE billing.BillingOutbox;
    PRINT 'Dropped table billing.BillingOutbox.';
END
ELSE
BEGIN
    PRINT 'Table billing.BillingOutbox does not exist — skipped.';
END
GO

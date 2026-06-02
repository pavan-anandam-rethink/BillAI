-- ============================================================================
-- Script: 002-outbox-table.rollback.sql
-- Purpose: Drops the BillingOutboxMessage table created in 002-outbox-table.sql.
-- Safe to run when: OutboxPublisherWorker is disabled and all outbox rows have
-- been processed (ProcessedOnUtc IS NOT NULL for all rows).
-- ============================================================================

-- Drain check: warn if unprocessed events exist before dropping.
DECLARE @UnprocessedCount INT;
SELECT @UnprocessedCount = COUNT(1)
FROM [dbo].[BillingOutboxMessage]
WHERE [ProcessedOnUtc] IS NULL;

IF @UnprocessedCount > 0
BEGIN
    RAISERROR(
        'Rollback blocked: %d unprocessed outbox message(s) remain. Drain the outbox before dropping the table.',
        16,
        1,
        @UnprocessedCount
    );
END
ELSE
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.objects
        WHERE object_id = OBJECT_ID(N'[dbo].[BillingOutboxMessage]')
          AND type_desc = 'USER_TABLE'
    )
    BEGIN
        DROP TABLE [dbo].[BillingOutboxMessage];
        PRINT 'Dropped BillingOutboxMessage table.';
    END
    ELSE
    BEGIN
        PRINT 'BillingOutboxMessage table does not exist — skipping drop.';
    END
END
GO

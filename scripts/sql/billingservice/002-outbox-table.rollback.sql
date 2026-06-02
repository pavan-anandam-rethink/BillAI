/*
Rollback script for BillingService outbox table (002-outbox-table.sql).

Drops the BillingOutbox table and its indexes.
Only execute this if the outbox worker has been disabled and all
unprocessed rows have been drained or confirmed as non-critical.
*/

IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.BillingOutbox')
      AND type = N'U'
)
BEGIN
    DROP TABLE dbo.BillingOutbox;
    PRINT 'Dropped dbo.BillingOutbox';
END;
ELSE
BEGIN
    PRINT 'dbo.BillingOutbox does not exist; nothing to roll back.';
END;

/*
Rollback script for 002-billing-outbox-table.sql.

Drops the BillingOutbox table and its indexes.
Only run after draining in-flight outbox rows and disabling the EnableOutboxPublisher flag.
*/

IF EXISTS (
    SELECT 1 FROM sys.objects WHERE name = 'BillingOutbox' AND type = 'U'
)
BEGIN
    DROP TABLE dbo.BillingOutbox;
END;

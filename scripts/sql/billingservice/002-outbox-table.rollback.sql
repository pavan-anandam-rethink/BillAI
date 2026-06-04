/*
Rollback script for 002-outbox-table.sql.
Drops the billing.BillingOutbox table.
Only run this if you need to revert the outbox table migration.
*/

IF OBJECT_ID(N'billing.BillingOutbox', 'U') IS NOT NULL
BEGIN
    DROP TABLE billing.BillingOutbox;
END;

/*
Rollback for: scripts/sql/billingservice/002-outbox-table.sql

Drops the billing.OutboxMessages table and the billing schema if it no longer contains
any other objects.

WARNING: This will permanently delete all unprocessed outbox messages.
Drain the outbox or verify it is empty before executing this rollback in production.
*/

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'billing.OutboxMessages') AND type = N'U')
BEGIN
    DROP TABLE billing.OutboxMessages;
END;

-- Drop the schema only when it is now empty
IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'billing')
   AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE schema_id = SCHEMA_ID(N'billing'))
BEGIN
    DROP SCHEMA billing;
END;

/*
Rollback script for BillingService outbox table (002-outbox-table.sql).

Drops the [billing].[OutboxMessages] table and its indexes.
Only run this if the forward migration needs to be fully reverted.
Drain the outbox worker and stop the BillingService before executing.
*/

IF EXISTS (
    SELECT 1
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'billing' AND t.name = 'OutboxMessages'
)
BEGIN
    DROP TABLE [billing].[OutboxMessages];
END;
GO

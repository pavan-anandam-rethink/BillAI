/*
Rollback script for 002-outbox-table.sql.

Drops the BillingOutbox table and all dependent indexes.
Execute ONLY when rolling back the outbox feature; never run in production without draining
the outbox worker and verifying there are no unprocessed rows.
*/

IF EXISTS (
    SELECT 1
    FROM   sys.tables
    WHERE  name = 'BillingOutbox'
      AND  SCHEMA_NAME(schema_id) = 'dbo'
)
BEGIN
    DROP TABLE dbo.BillingOutbox;
END;

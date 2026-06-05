-- 002-outbox-table.rollback.sql
-- Drops the BillingOutbox and BlobMetadata tables created by 002-outbox-table.sql.
-- Run this ONLY to roll back the outbox migration. Existing outbox rows will be lost.

IF EXISTS (
    SELECT 1
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'billing' AND t.name = 'BillingOutbox'
)
BEGIN
    DROP TABLE billing.BillingOutbox;
    PRINT 'Dropped billing.BillingOutbox.';
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'billing' AND t.name = 'BlobMetadata'
)
BEGIN
    DROP TABLE billing.BlobMetadata;
    PRINT 'Dropped billing.BlobMetadata.';
END;
GO

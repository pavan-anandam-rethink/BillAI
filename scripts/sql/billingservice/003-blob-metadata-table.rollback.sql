/*
Rollback script for 003-blob-metadata-table.sql.
Drops the billing.BillingBlobMetadata table.
Only run this if you need to revert the blob metadata migration.
*/

IF OBJECT_ID(N'billing.BillingBlobMetadata', 'U') IS NOT NULL
BEGIN
    DROP TABLE billing.BillingBlobMetadata;
END;

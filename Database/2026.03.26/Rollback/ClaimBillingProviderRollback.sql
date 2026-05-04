-- Rollback Script: Drop ClaimBillingProvider table if it exists
IF OBJECT_ID('dbo.ClaimBillingProvider', 'U') IS NOT NULL
BEGIN
    -- Drop the table (automatically drops FKs and indexes)
    DROP TABLE dbo.ClaimBillingProvider;
    PRINT 'Rollback: ClaimBillingProvider table has been dropped.';
END
ELSE
BEGIN
    PRINT 'Rollback: ClaimBillingProvider table does not exist.';
END
GO
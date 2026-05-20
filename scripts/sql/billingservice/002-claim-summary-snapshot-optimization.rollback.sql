/*
Rollback for claim summary snapshot optimization indexes.
*/

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClearingHouseResponseDetails_ClaimId_ResponseFileTypeId_DateDeleted'
      AND object_id = OBJECT_ID(N'dbo.ClearingHouseResponseDetails')
)
BEGIN
    DROP INDEX IX_ClearingHouseResponseDetails_ClaimId_ResponseFileTypeId_DateDeleted
    ON dbo.ClearingHouseResponseDetails;
END;

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_Account_TabStatus_ClaimNumber_Claim'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_Account_TabStatus_ClaimNumber_Claim
    ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_Account_TabStatus_Dos_Claim'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_Account_TabStatus_Dos_Claim
    ON dbo.ClaimSummarySnapshot;
END;

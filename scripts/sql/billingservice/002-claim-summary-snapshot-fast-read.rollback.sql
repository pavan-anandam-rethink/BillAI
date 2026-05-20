/*
Rollback for BillingService GetClaimsByAccountInfoId snapshot fast-read indexes.

The procedure body is not dropped here because this script replaces an existing
stored procedure and the prior body is not stored in this repository.
*/

IF OBJECT_ID(N'dbo.ClearingHouseResponseDetails', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClearingHouseResponseDetails_Claim_ResponseType' AND object_id = OBJECT_ID(N'dbo.ClearingHouseResponseDetails'))
    BEGIN
        DROP INDEX IX_ClearingHouseResponseDetails_Claim_ResponseType ON dbo.ClearingHouseResponseDetails;
    END;
END;

IF OBJECT_ID(N'dbo.ClaimSummarySnapshot', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Assignee_Filter' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Assignee_Filter ON dbo.ClaimSummarySnapshot;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Funder_Filter' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Funder_Filter ON dbo.ClaimSummarySnapshot;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Patient_Filter' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Patient_Filter ON dbo.ClaimSummarySnapshot;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_BilledDate' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Flag_BilledDate ON dbo.ClaimSummarySnapshot;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_DosStart' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Flag_DosStart ON dbo.ClaimSummarySnapshot;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_ClaimNumber' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Flag_ClaimNumber ON dbo.ClaimSummarySnapshot;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_Status_ClaimId' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        DROP INDEX IX_ClaimSummarySnapshot_Account_Flag_Status_ClaimId ON dbo.ClaimSummarySnapshot;
    END;
END;

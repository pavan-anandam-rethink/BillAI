/*
Rollback for BillingService fast claim summary read indexes.

The stored procedure body is intentionally not rolled back here because this
repository does not contain the previous GetClaimsByAccountInfoId definition.
Restore the prior procedure from the deployment artifact that originally
created it if a procedure rollback is required.
*/

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusProvider' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusProvider ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusReason' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusReason ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusLocation' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusLocation ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusAssignee' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusAssignee ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusFunder' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusFunder ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusPatient' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusPatient ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusDos' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusDos ON dbo.ClaimSummarySnapshot;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusClaim' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusClaim ON dbo.ClaimSummarySnapshot;
END;

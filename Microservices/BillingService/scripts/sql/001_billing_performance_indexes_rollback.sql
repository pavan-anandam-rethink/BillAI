/*
    Rollback for 001_billing_performance_indexes.sql.
    Drops only indexes created by the companion additive migration.
*/

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Claims_AccountInfoId_Status_MemberId' AND object_id = OBJECT_ID(N'dbo.Claims'))
BEGIN
    DROP INDEX IX_Claims_AccountInfoId_Status_MemberId ON dbo.Claims;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimChargeEntries_ClaimId_ServiceDate' AND object_id = OBJECT_ID(N'dbo.ClaimChargeEntries'))
BEGIN
    DROP INDEX IX_ClaimChargeEntries_ClaimId_ServiceDate ON dbo.ClaimChargeEntries;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentClaims_ClaimId_PaymentId' AND object_id = OBJECT_ID(N'dbo.PaymentClaims'))
BEGIN
    DROP INDEX IX_PaymentClaims_ClaimId_PaymentId ON dbo.PaymentClaims;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PatientInvoices_AccountInfoId_PatientId_Status' AND object_id = OBJECT_ID(N'dbo.PatientInvoices'))
BEGIN
    DROP INDEX IX_PatientInvoices_AccountInfoId_PatientId_Status ON dbo.PatientInvoices;
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimSubmissions_ClaimId_CreatedDate' AND object_id = OBJECT_ID(N'dbo.ClaimSubmissions'))
BEGIN
    DROP INDEX IX_ClaimSubmissions_ClaimId_CreatedDate ON dbo.ClaimSubmissions;
END
GO

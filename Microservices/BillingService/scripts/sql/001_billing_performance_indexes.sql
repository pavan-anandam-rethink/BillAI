/*
    BillingService non-breaking performance indexes.

    These scripts are additive and schema-compatible. Validate actual table and column names
    against the target Azure SQL database before execution because this repository uses shared
    EF entities outside the BillingService folder.
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Claims', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Claims', N'AccountInfoId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Claims', N'MemberId') IS NOT NULL
   AND COL_LENGTH(N'dbo.Claims', N'Status') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Claims_AccountInfoId_Status_MemberId' AND object_id = OBJECT_ID(N'dbo.Claims'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Claims_AccountInfoId_Status_MemberId
        ON dbo.Claims (AccountInfoId, Status, MemberId)
        INCLUDE (ClaimId, DateOfService, TotalChargeAmount, UpdatedDate);
END
GO

IF OBJECT_ID(N'dbo.ClaimChargeEntries', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ClaimChargeEntries', N'ClaimId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimChargeEntries_ClaimId_ServiceDate' AND object_id = OBJECT_ID(N'dbo.ClaimChargeEntries'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimChargeEntries_ClaimId_ServiceDate
        ON dbo.ClaimChargeEntries (ClaimId, ServiceDate)
        INCLUDE (ChargeAmount, Units, BillingCodeId);
END
GO

IF OBJECT_ID(N'dbo.PaymentClaims', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PaymentClaims', N'ClaimId') IS NOT NULL
   AND COL_LENGTH(N'dbo.PaymentClaims', N'PaymentId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentClaims_ClaimId_PaymentId' AND object_id = OBJECT_ID(N'dbo.PaymentClaims'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentClaims_ClaimId_PaymentId
        ON dbo.PaymentClaims (ClaimId, PaymentId)
        INCLUDE (PaidAmount, AdjustmentAmount, CreatedDate);
END
GO

IF OBJECT_ID(N'dbo.PatientInvoices', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientInvoices', N'AccountInfoId') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientInvoices', N'PatientId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PatientInvoices_AccountInfoId_PatientId_Status' AND object_id = OBJECT_ID(N'dbo.PatientInvoices'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PatientInvoices_AccountInfoId_PatientId_Status
        ON dbo.PatientInvoices (AccountInfoId, PatientId, Status)
        INCLUDE (InvoiceId, InvoiceDate, TotalAmount, BalanceAmount);
END
GO

IF OBJECT_ID(N'dbo.ClaimSubmissions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ClaimSubmissions', N'ClaimId') IS NOT NULL
   AND COL_LENGTH(N'dbo.ClaimSubmissions', N'CreatedDate') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ClaimSubmissions_ClaimId_CreatedDate' AND object_id = OBJECT_ID(N'dbo.ClaimSubmissions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSubmissions_ClaimId_CreatedDate
        ON dbo.ClaimSubmissions (ClaimId, CreatedDate DESC)
        INCLUDE (Status, ClearingHouseId, FunderId);
END
GO

/*
Safe, non-breaking index optimizations for billing read-heavy workloads.
Review exact table names in target DB before execution.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Claims_AccountInfoId_Status_ServiceDate'
      AND object_id = OBJECT_ID('dbo.Claims')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Claims_AccountInfoId_Status_ServiceDate
    ON dbo.Claims (AccountInfoId, ClaimStatus, DateOfService DESC)
    INCLUDE (ClientInfoId, FunderId, TotalAmount, BalanceAmount, UpdatedDate);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PatientInvoices_AccountInfoId_CreatedUtc'
      AND object_id = OBJECT_ID('dbo.PatientInvoices')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PatientInvoices_AccountInfoId_CreatedUtc
    ON dbo.PatientInvoices (AccountInfoId, CreatedUtc DESC)
    INCLUDE (InvoiceId, ClientInfoId, InvoiceStatus, TotalAmount, BalanceAmount);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PaymentClaims_ClaimId_PostedDate'
      AND object_id = OBJECT_ID('dbo.PaymentClaims')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentClaims_ClaimId_PostedDate
    ON dbo.PaymentClaims (ClaimId, PostedDate DESC)
    INCLUDE (PaymentId, PaidAmount, AdjustmentAmount, WriteOffAmount);
END
GO


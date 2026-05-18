IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Claims_AccountInfoId_Status_ServiceDate'
      AND object_id = OBJECT_ID('dbo.Claims')
)
BEGIN
    DROP INDEX IX_Claims_AccountInfoId_Status_ServiceDate ON dbo.Claims;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PatientInvoices_AccountInfoId_CreatedUtc'
      AND object_id = OBJECT_ID('dbo.PatientInvoices')
)
BEGIN
    DROP INDEX IX_PatientInvoices_AccountInfoId_CreatedUtc ON dbo.PatientInvoices;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PaymentClaims_ClaimId_PostedDate'
      AND object_id = OBJECT_ID('dbo.PaymentClaims')
)
BEGIN
    DROP INDEX IX_PaymentClaims_ClaimId_PostedDate ON dbo.PaymentClaims;
END
GO


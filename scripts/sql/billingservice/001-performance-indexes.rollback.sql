/*
Rollback for BillingService additive performance indexes.
*/

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PatientInvoice_AccountInfoId_DateDeleted_Status' AND object_id = OBJECT_ID(N'dbo.PatientInvoice'))
BEGIN
    DROP INDEX IX_PatientInvoice_AccountInfoId_DateDeleted_Status ON dbo.PatientInvoice;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PaymentClaimServiceLine_ChargeId_DateDeleted' AND object_id = OBJECT_ID(N'dbo.PaymentClaimServiceLine'))
BEGIN
    DROP INDEX IX_PaymentClaimServiceLine_ChargeId_DateDeleted ON dbo.PaymentClaimServiceLine;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PaymentClaim_ClaimId_DateDeleted' AND object_id = OBJECT_ID(N'dbo.PaymentClaim'))
BEGIN
    DROP INDEX IX_PaymentClaim_ClaimId_DateDeleted ON dbo.PaymentClaim;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payment_AccountInfoId_DateDeleted_DepositDate' AND object_id = OBJECT_ID(N'dbo.Payment'))
BEGIN
    DROP INDEX IX_Payment_AccountInfoId_DateDeleted_DepositDate ON dbo.Payment;
END;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Claim_AccountInfoId_DateDeleted_Status' AND object_id = OBJECT_ID(N'dbo.Claim'))
BEGIN
    DROP INDEX IX_Claim_AccountInfoId_DateDeleted_Status ON dbo.Claim;
END;

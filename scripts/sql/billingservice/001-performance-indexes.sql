/*
BillingService additive performance indexes.

Review actual table names and query plans in the target Azure SQL database before execution.
All statements are idempotent and intended to preserve schema compatibility.
*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Claim_AccountInfoId_DateDeleted_Status' AND object_id = OBJECT_ID(N'dbo.Claim'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Claim_AccountInfoId_DateDeleted_Status
    ON dbo.Claim (AccountInfoId, DateDeleted, Status)
    INCLUDE (Id, ClaimIdentifier, ClientId, FunderId, DateCreated, DateModified)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payment_AccountInfoId_DateDeleted_DepositDate' AND object_id = OBJECT_ID(N'dbo.Payment'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payment_AccountInfoId_DateDeleted_DepositDate
    ON dbo.Payment (AccountInfoId, DateDeleted, DepositDate)
    INCLUDE (Id, PaymentIdentifier, Amount, PaymentMethodId, DateCreated)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PaymentClaim_ClaimId_DateDeleted' AND object_id = OBJECT_ID(N'dbo.PaymentClaim'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentClaim_ClaimId_DateDeleted
    ON dbo.PaymentClaim (ClaimId, DateDeleted)
    INCLUDE (Id, PaymentId, Status, DateCreated)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PaymentClaimServiceLine_ChargeId_DateDeleted' AND object_id = OBJECT_ID(N'dbo.PaymentClaimServiceLine'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentClaimServiceLine_ChargeId_DateDeleted
    ON dbo.PaymentClaimServiceLine (ClaimChargeEntryId, DateDeleted)
    INCLUDE (Id, PaymentClaimId, PaidAmount, AllowedAmount, DateCreated)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PatientInvoice_AccountInfoId_DateDeleted_Status' AND object_id = OBJECT_ID(N'dbo.PatientInvoice'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PatientInvoice_AccountInfoId_DateDeleted_Status
    ON dbo.PatientInvoice (AccountInfoId, DateDeleted, Status)
    INCLUDE (Id, InvoiceNumber, ClientId, InvoiceDate, TotalAmount)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

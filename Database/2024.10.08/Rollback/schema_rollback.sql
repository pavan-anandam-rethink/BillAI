SET NOEXEC OFF
SET NOCOUNT ON
SET NUMERIC_ROUNDABORT OFF
GO

SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO

SET XACT_ABORT ON
GO

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE

BEGIN TRANSACTION

IF @@ERROR <> 0 SET NOEXEC ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.PaymentClaim') AND type in (N'U'))
BEGIN
	IF EXISTS (SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[PaymentClaim]') AND name = 'placeOfService')
	BEGIN
		ALTER TABLE dbo.[PaymentClaim] DROP COLUMN placeOfService
	END
	IF EXISTS (SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[PaymentClaim]') AND name = 'patientId')
	BEGIN
		ALTER TABLE dbo.[PaymentClaim] DROP COLUMN patientId
	END
	IF EXISTS (SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[PaymentClaim]') AND name = 'renderingProviderId')
	BEGIN
		ALTER TABLE dbo.[PaymentClaim] DROP COLUMN renderingProviderId
	END
END
GO

IF OBJECT_ID('dbo.PatientInvoice','U') IS NOT NULL
BEGIN

		DROP TABLE PatientInvoiceStatus
END;

IF OBJECT_ID('dbo.PatientInvoiceDetails','U') IS NOT NULL
BEGIN
		DROP TABLE PatientInvoiceDetails
END;

IF OBJECT_ID('dbo.PatientInvoice','U') IS NOT NULL
BEGIN

		DROP TABLE PatientInvoice
END;

IF OBJECT_ID('dbo.GetPatientInvoiceCreationDetails','P') IS NOT NULL
BEGIN		
		DROP PROC GetPatientInvoiceCreationDetails
END;


IF @@ERROR <> 0 SET NOEXEC ON
GO

COMMIT TRANSACTION
GO

DECLARE @Success AS BIT
SET @Success=1
SET NOEXEC OFF
IF (@Success = 1) PRINT 'Database update succeeded'
ELSE BEGIN
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
	PRINT 'The database update failed. ROLLBACK TRANSACTION'
END
GO
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

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.PaymentMethod') AND type in (N'U'))
BEGIN
	--INSERT INTO [dbo].[PaymentMethod]([Id],[Name],[dateCreated],[dateLastModified],[CreatedBy]) VALUES
	--	(1,'Cash',GETDATE(),GETDATE(),0),(2,'Check',GETDATE(),GETDATE(),0),(3,'ACH',GETDATE(),GETDATE(),0),(4,'Transfer',GETDATE(),GETDATE(),0),
	--	(5,'Credit Card',GETDATE(),GETDATE(),0),(6,'Non-Payment',GETDATE(),GETDATE(),0),(7,'FSA/HSA',GETDATE(),GETDATE(),0)

	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 1
	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 2
	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 3
	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 4
	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 5
	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 6
	DELETE FROM [dbo].[PaymentMethod] WHERE Id = 7
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimResponseFileType') AND type in (N'U'))
BEGIN
	--INSERT INTO [dbo].[ClaimResponseFileType] ([Id],[FileType]) VALUES (1,'File999'),(2,'File277')
	DELETE FROM [dbo].[ClaimResponseFileType] WHERE Id = 1
	DELETE FROM [dbo].[ClaimResponseFileType] WHERE Id = 2
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimActions') AND type in (N'U'))
BEGIN
	--INSERT INTO [dbo].[ClaimActions] ([Id],[name]) VALUES (28,'Claim Processing')
	DELETE FROM [dbo].[ClaimActions] WHERE id = 28
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimErrorMessages') AND type in (N'U'))
BEGIN
	--UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 7, shortDescription = 'Clearing House - Clearinghouse Rejected' WHERE id = 73
	UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 22, shortDescription = 'Electronic Remittance Advice - Clearinghouse Rejected' WHERE id = 73
	
	--UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 7, shortDescription = 'Clearing House - Funder Accepted With Errors' WHERE id = 74
	UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 22, shortDescription = 'Electronic Remittance Advice - Funder Accepted With Errors' WHERE id = 74
	
	--UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 7, shortDescription = 'Clearing House - Funder Rejected' WHERE id = 75
	UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 22, shortDescription = 'Electronic Remittance Advice - Funder Rejected' WHERE id = 75
	
	--UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 7, shortDescription = 'Clearing House - Funder Denied' WHERE id = 76
	UPDATE dbo.ClaimErrorMessages SET claimErrorCategoryId = 22, shortDescription = 'Electronic Remittance Advice - Funder Denied' WHERE id = 76
END
GO

--*********************************************************************************************************************************************************
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimHistoryActions') AND type in (N'U'))
BEGIN
	--INSERT INTO [dbo].[ClaimHistoryActions] ([Id],[name],[description]) VALUES (56, 'ClaimResponseAccepted999','999 Accepted')
	DELETE FROM [dbo].[ClaimHistoryActions] WHERE id = 56
	--INSERT INTO [dbo].[ClaimHistoryActions] ([Id],[name],[description]) VALUES (57, 'ClaimResponseRejected999','999 Rejected')
	DELETE FROM [dbo].[ClaimHistoryActions] WHERE id = 57
	--INSERT INTO [dbo].[ClaimHistoryActions] ([Id],[name],[description]) VALUES (58, 'ClaimResponseAccepted277','277 Accepted')
	DELETE FROM [dbo].[ClaimHistoryActions] WHERE id = 58
	--INSERT INTO [dbo].[ClaimHistoryActions] ([Id],[name],[description]) VALUES (59, 'ClaimResponseRejected277','277 Rejected')
	DELETE FROM [dbo].[ClaimHistoryActions] WHERE id = 59
	--INSERT INTO [dbo].[ClaimHistoryActions] ([Id],[name],[description]) VALUES (60, 'ClaimResponseReceived277','277 Received')
	DELETE FROM [dbo].[ClaimHistoryActions] WHERE id = 60
END
GO
--*********************************************************************************************************************************************************
--CHAGNES BY CHETAN - 08072024
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimStatus') AND type in (N'U'))
BEGIN
	--INSERT INTO dbo.ClaimStatus VALUES (15, 'Clearinghouse - Accepted', GETDATE(), GETDATE(), null)
	DELETE FROM [dbo].[ClaimStatus] WHERE id = 15 
	--INSERT INTO dbo.ClaimStatus VALUES (16, 'Funder - Accepted', GETDATE(), GETDATE(), null)
	DELETE FROM [dbo].[ClaimStatus] WHERE id = 16
	--INSERT INTO dbo.ClaimStatus VALUES (17, 'Funder - Received', GETDATE(), GETDATE(), null)
	DELETE FROM [dbo].[ClaimStatus] WHERE id = 17

	UPDATE DBO.ClaimStatus SET [name] = 'Rejected - Clearinghouse' WHERE id = 8
	UPDATE DBO.ClaimStatus SET [name] = 'Rejected - Funder' WHERE id = 9
	UPDATE DBO.ClaimStatus SET [name] = 'Accepted - Clearinghouse' WHERE id = 15
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimSubmissionStatus') AND type in (N'U'))
BEGIN
	--INSERT INTO dbo.ClaimSubmissionStatus VALUES (12, 'ClearinghouseAccepted', GETDATE(), GETDATE(), null)
	DELETE FROM [dbo].[ClaimSubmissionStatus] WHERE id = 12 
END
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimErrorMessages') AND type in (N'U'))
BEGIN
	--UPDATE dbo.ClaimErrorMessages set severity = 4 where claimErrorCategoryId = 7
	UPDATE dbo.ClaimErrorMessages set severity = 2 where claimErrorCategoryId = 7
END
GO
--*********************************************************************************************************************************************************


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
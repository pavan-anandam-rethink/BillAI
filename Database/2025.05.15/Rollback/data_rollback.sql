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

--Rollback
BEGIN TRANSACTION;

	-- Delete the records if present with newly added History from History tables Ids
	delete from dbo.ClaimHistory where claimHistoryActionId IN (62, 63, 64, 65, 66, 67);
	delete from dbo.ClaimHistory where claimActionId IN (31,30);
	delete from dbo.ClaimHistory where claimHistoryFieldId IN (21,22);

	-- Delete inserted records
	DELETE FROM dbo.ClaimActions WHERE id IN (30, 31);
	DELETE FROM dbo.ClaimHistoryActions WHERE id IN (62, 63, 64, 65, 66, 67);
	DELETE FROM dbo.ClaimHistoryFields WHERE id IN (21, 22);

 
	-- Revert updates 
	UPDATE dbo.ClaimHistoryActions 
	SET description = 'Adjustment Applied.' 
	WHERE id = 1;
	UPDATE dbo.ClaimHistoryActions 
	SET name = 'WriteoffWithReasonCode', description = 'Claim# Writeoff settlement reason code "@newValue".' 
	WHERE id = 41;
	UPDATE dbo.ClaimHistoryActions 
	SET name = 'WriteoffWithAmount', description = 'Claim# Writeoff "Amount = @newValue"' 
	WHERE id = 42;
COMMIT TRANSACTION;

GO 
 


IF @@ERROR <> 0 SET NOEXEC ON
GO


GO
COMMIT TRANSACTION
GO

DECLARE @Success AS BIT
SET @Success=1
SET NOEXEC OFF
IF (@Success = 1) PRINT 'Database insertion  succeeded'
ELSE BEGIN
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
	PRINT 'The database insertion failed. ROLLBACK TRANSACTION'
END
GO
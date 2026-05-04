----###############################################
------ 257311 Searchable Adjustment Codes Rollback
----################################################

	
BEGIN TRY
    BEGIN TRANSACTION;
    
	ALTER TABLE [dbo].[PaymentAdjustmentReason]
	DROP CONSTRAINT DF__PaymentAdjustmentReason__Default__IsDefault;

	-- Delete the records if present with newly added History from History tables Ids
	ALTER TABLE [dbo].[PaymentAdjustmentReason] DROP COLUMN IsDefault;

    COMMIT TRANSACTION;
	SELECT 'TRANSACTION COMPLETED'
	
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
	BEGIN
        ROLLBACK TRANSACTION;
		SELECT 'ROLLBACK COMPLETED'
	END

    SELECT 'Error occurred: ' + ERROR_MESSAGE();
    --THROW;
END CATCH;

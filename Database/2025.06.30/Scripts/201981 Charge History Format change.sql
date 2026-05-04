----###############################################
------ 201981 HISTORY NOT CREATED
----################################################

BEGIN TRY
    BEGIN TRANSACTION;
    
    -- Add column if it doesn't exist
    IF EXISTS (
        SELECT 1
        FROM ClaimHistoryActions
        WHERE ID IN (68, 69)
    )
    BEGIN
		UPDATE ClaimHistoryActions
		SET name='ChargeEntryNoteDescriptionAdded',description = 'Charge Entry note = "@newValue" added'
		WHERE ID = 68;

		UPDATE ClaimHistoryActions
		SET name='ChargeEntryNoteDescriptionRemoved',description = 'Charge Entry note = "@newValue" removed'
		WHERE ID = 69;
    END

   
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
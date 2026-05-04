----###############################################
------ 201981 HISTORY NOT CREATED
----################################################
BEGIN TRY
    BEGIN TRANSACTION;
    -- Add column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1
        FROM ClaimHistoryActions
        WHERE ID = 69
          AND NAME = 'ChargeEntryNoteDescRemoved'
          AND DESCRIPTION = 'Charge Entry "note = @newValue" removed'
    )
    BEGIN
		insert into ClaimHistoryActions(id,name, description)
		values(68,'ChargeEntryNoteDescAdded','Charge Entry "note = @newValue" added')
 
        insert into ClaimHistoryActions (id,name, description) 
		values(69,'ChargeEntryNoteDescRemoved','Charge Entry "note = @newValue" removed')
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
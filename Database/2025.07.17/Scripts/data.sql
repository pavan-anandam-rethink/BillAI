----########################################################
------ 262039 In history adjustment code should be displayed
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;
           BEGIN
		UPDATE ClaimHistoryActions
                SET description = 'Adjustment updated = "' + '@newValue' + '"'
                WHERE id = 66;
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
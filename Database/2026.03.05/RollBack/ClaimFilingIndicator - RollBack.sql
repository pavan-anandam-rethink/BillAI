----########################################################
---- ROLLBACK: Drop ClaimFilingIndicator Master Table
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    ------------------------------------------------------------
    -- 1️⃣ Check If Table Exists
    ------------------------------------------------------------
    IF EXISTS (
        SELECT 1 
        FROM sys.tables 
        WHERE name = 'ClaimFilingIndicator'
    )
    BEGIN
	
		DROP TABLE dbo.ClaimFilingIndicator;
		
    END

    ------------------------------------------------------------
    -- Commit Transaction
    ------------------------------------------------------------
    COMMIT TRANSACTION;

    SELECT 'ROLLBACK COMPLETED: ClaimFilingIndicator table dropped successfully.' AS Result;

END TRY
BEGIN CATCH

    ------------------------------------------------------------
    -- Rollback If Error
    ------------------------------------------------------------
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;

        SELECT 'ROLLBACK FAILED: Error occurred while dropping ClaimFilingIndicator table.' AS Result;
    END

    SELECT 'Error occurred: ' + ERROR_MESSAGE() AS ErrorMessage;

    -- Optional:
    -- THROW;

END CATCH;
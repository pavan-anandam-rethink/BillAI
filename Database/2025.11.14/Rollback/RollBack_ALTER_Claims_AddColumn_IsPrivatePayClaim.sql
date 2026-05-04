----########################################################
------ Rollback: Remove column [IsPrivatePayClaim] from [dbo].[Claims]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;

        -- Check if the column exists before dropping
        IF COL_LENGTH('dbo.Claims', 'IsPrivatePayClaim') IS NOT NULL
        BEGIN
            -- Drop the column
            ALTER TABLE [dbo].[Claims]
            DROP COLUMN IsPrivatePayClaim;

            SELECT 'Column [IsPrivatePayClaim] removed successfully from [dbo].[Claims].' AS Message;
        END
        ELSE
        BEGIN
            SELECT 'Column [IsPrivatePayClaim] does not exist in [dbo].[Claims]. Nothing to rollback.' AS Message;
        END

    COMMIT TRANSACTION;
    SELECT 'TRANSACTION COMPLETED' AS Status;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT 'ROLLBACK COMPLETED' AS Status;
    END
    SELECT 'Error occurred during rollback: ' + ERROR_MESSAGE() AS ErrorMessage;
END CATCH;

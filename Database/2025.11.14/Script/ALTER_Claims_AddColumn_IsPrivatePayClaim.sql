----########################################################
------ Add new column [IsPrivatePayClaim] to [dbo].[Claims]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;

        -- Check if the column already exists
        IF COL_LENGTH('dbo.Claims', 'IsPrivatePayClaim') IS NULL
        BEGIN
            -- Add the new column
            ALTER TABLE [dbo].[Claims]
            ADD IsPrivatePayClaim BIT NULL;

            SELECT 'Column [IsPrivatePayClaim] added successfully to [dbo].[Claims].' AS Message;
        END
        ELSE
        BEGIN
            SELECT 'Column [IsPrivatePayClaim] already exists in [dbo].[Claims].' AS Message;
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
    SELECT 'Error occurred: ' + ERROR_MESSAGE() AS ErrorMessage;
END CATCH;

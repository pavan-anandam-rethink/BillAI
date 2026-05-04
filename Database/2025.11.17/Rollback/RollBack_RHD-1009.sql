----########################################################
------ Rollback: Remove column [AccountInfoId] from [dbo].[ClaimAppointmentLink]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;

        -- Check if the column exists before dropping
        IF COL_LENGTH('dbo.ClaimAppointmentLink', 'AccountInfoId') IS NOT NULL
        BEGIN
            -- Drop the column
            ALTER TABLE [dbo].[ClaimAppointmentLink]
            DROP COLUMN AccountInfoId;

            PRINT 'Column [AccountInfoId] removed successfully from [dbo].[ClaimAppointmentLink].';
        END
        ELSE
        BEGIN
            PRINT 'Column [AccountInfoId] does not exist in [dbo].[ClaimAppointmentLink]. Nothing to rollback.';
        END

		IF EXISTS ( SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimAppointmentLink_AccountInfoId' AND object_id = OBJECT_ID('dbo.ClaimAppointmentLink') ) 
		BEGIN 
		DROP INDEX [IX_ClaimAppointmentLink_AccountInfoId]
		ON [dbo].[ClaimAppointmentLink];
		END

    COMMIT TRANSACTION;
    PRINT 'TRANSACTION COMPLETED';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'ROLLBACK COMPLETED';
    END
    PRINT 'Error occurred during rollback: ' + ERROR_MESSAGE();
END CATCH;

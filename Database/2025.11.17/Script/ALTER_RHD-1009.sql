----########################################################
------ Add new column [AccountInfoId] to [dbo].[ClaimAppointmentLink]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;
	SET NOCOUNT ON;               -- Prevent extra rowcount messages
    SET XACT_ABORT ON;            -- Auto-rollback on severe errors
        -- Check if the column already exists
        IF COL_LENGTH('[dbo].[ClaimAppointmentLink]', 'AccountInfoId') IS NULL
        BEGIN
            -- Add the new column
            ALTER TABLE [dbo].[ClaimAppointmentLink]
            ADD AccountInfoId INT NULL;

            PRINT 'Column [AccountInfoId] added successfully to [dbo].[ClaimAppointmentLink].';
        END
        ELSE
        BEGIN
            PRINT 'Column [AccountInfoId] already exists in [dbo].[ClaimAppointmentLink].';
        END

		IF NOT EXISTS (
            SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimAppointmentLink_AccountInfoId' AND object_id = OBJECT_ID('dbo.ClaimAppointmentLink'))
	    BEGIN
            CREATE INDEX [IX_ClaimAppointmentLink_AccountInfoId]
            ON [dbo].[ClaimAppointmentLink] ([AccountInfoId]);
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
    PRINT 'Error occurred: ' + ERROR_MESSAGE();
END CATCH;

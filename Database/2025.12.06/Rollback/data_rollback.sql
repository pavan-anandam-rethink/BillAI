----########################################################
------ Rollback: Remove column [GuarantorContactId] from [dbo].[UnallocatedPayments]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;

        -- Check if the column exists before dropping
        IF COL_LENGTH('dbo.UnallocatedPayments', 'GuarantorContactId') IS NOT NULL
        BEGIN
            -- Drop the column
            ALTER TABLE [dbo].[UnallocatedPayments]
            DROP COLUMN [GuarantorContactId];

            PRINT 'Column [GuarantorContactId] removed successfully from [dbo].[UnallocatedPayments].';
        END
        ELSE
        BEGIN
            PRINT 'Column [GuarantorContactId] does not exist in [dbo].[UnallocatedPayments]. Nothing to rollback.';
        END

        -- Drop the index if it exists
        IF EXISTS (
            SELECT 1 
            FROM sys.indexes 
            WHERE name = 'IX_UnallocatedPayments_GuarantorContactId'
              AND object_id = OBJECT_ID('dbo.UnallocatedPayments')
        )
        BEGIN
            DROP INDEX [IX_UnallocatedPayments_GuarantorContactId]
            ON [dbo].[UnallocatedPayments];

            PRINT 'Index [IX_UnallocatedPayments_GuarantorContactId] removed successfully.';
        END
        ELSE
        BEGIN
            PRINT 'Index [IX_UnallocatedPayments_GuarantorContactId] does not exist. Nothing to rollback.';
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

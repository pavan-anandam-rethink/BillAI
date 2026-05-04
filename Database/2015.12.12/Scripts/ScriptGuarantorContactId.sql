----########################################################
------ Add new column [GuarantorContactId] to [dbo].[UnallocatedPayments]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;
    SET NOCOUNT ON;               -- Prevent extra rowcount messages
    SET XACT_ABORT ON;            -- Auto-rollback on severe errors

    -- Check if the column already exists
    IF COL_LENGTH('[dbo].[UnallocatedPayments]', 'GuarantorContactId') IS NULL
    BEGIN
        -- Add the new column
        ALTER TABLE [dbo].[UnallocatedPayments]
        ADD [GuarantorContactId] INT NULL;

        PRINT 'Column [GuarantorContactId] added successfully to [dbo].[UnallocatedPayments].';
    END
    ELSE
    BEGIN
        PRINT 'Column [GuarantorContactId] already exists in [dbo].[UnallocatedPayments].';
    END

    -- Optional: Add index if it does not already exist
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.indexes 
        WHERE name = 'IX_UnallocatedPayments_GuarantorContactId'
          AND object_id = OBJECT_ID('dbo.UnallocatedPayments')
    )
    BEGIN
        CREATE INDEX [IX_UnallocatedPayments_GuarantorContactId]
        ON [dbo].[UnallocatedPayments] ([GuarantorContactId]);

        PRINT 'Index [IX_UnallocatedPayments_GuarantorContactId] created successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Index [IX_UnallocatedPayments_GuarantorContactId] already exists.';
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

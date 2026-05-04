SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Check if column exists before attempting to drop
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE name = 'FailureResponse'
          AND object_id = OBJECT_ID('dbo.Eligibility271Response')
    )
    BEGIN
       
        ALTER TABLE dbo.Eligibility271Response
        DROP COLUMN FailureResponse;

        PRINT 'Column FailureResponse dropped successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Column FailureResponse does not exist. No rollback required.';
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO

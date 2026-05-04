
/*=============================================================
Rollback Script
Change Name  : Remove index for Rendering Provider claim search
Table        : dbo.ClaimChargeEntries
Index Name   : IX_ClaimChargeEntries_Claim_RenderingProvider
=============================================================*/

BEGIN TRY
    BEGIN TRANSACTION

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_ClaimChargeEntries_Claim_RenderingProvider'
        AND object_id = OBJECT_ID('dbo.ClaimChargeEntries')
    )
    BEGIN
        PRINT 'Dropping index IX_ClaimChargeEntries_Claim_RenderingProvider';

        DROP INDEX IX_ClaimChargeEntries_Claim_RenderingProvider
        ON dbo.ClaimChargeEntries;

        PRINT 'Index dropped successfully';
    END
    ELSE
    BEGIN
        PRINT 'Index does not exist. No action required.';
    END

    COMMIT TRANSACTION
END TRY

BEGIN CATCH
    PRINT 'Error occurred during rollback. Rolling back transaction...'

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION

    THROW;
END CATCH
GO


----########################################################
---- Add RenderingProviderId Column to ClaimChargeEntries
----########################################################
BEGIN TRY

    BEGIN TRANSACTION;

    -- RenderingProviderId (Default = 0, Not Null)
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'ClaimChargeEntries'
          AND COLUMN_NAME = 'RenderingProviderId'
    )
    BEGIN
        ALTER TABLE dbo.ClaimChargeEntries
        ADD RenderingProviderId INT NOT NULL
            CONSTRAINT DF_ClaimChargeEntries_RenderingProviderId DEFAULT (0);
    END

    COMMIT TRANSACTION;

    SELECT 'SCHEMA UPDATE COMPLETED: RenderingProviderId column added successfully' AS Result;

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT 
        'SCHEMA UPDATE FAILED' AS Result,
        ERROR_MESSAGE() AS ErrorMessage;

END CATCH;

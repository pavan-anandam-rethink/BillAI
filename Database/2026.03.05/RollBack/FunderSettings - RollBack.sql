----########################################################
---- ROLLBACK: Drop FunderSettings Table
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    ------------------------------------------------------------
    -- 1️⃣ Check If Table Exists
    ------------------------------------------------------------
    IF EXISTS (
        SELECT 1 
        FROM sys.tables 
        WHERE name = 'FunderSettings'
    )
    BEGIN

        ------------------------------------------------------------
        -- 2️⃣ Drop Foreign Key (If Exists)
        ------------------------------------------------------------
        IF EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = 'FK_FunderSettings_ClaimFilingIndicator'
        )
        BEGIN
            ALTER TABLE dbo.FunderSettings
            DROP CONSTRAINT FK_FunderSettings_ClaimFilingIndicator;
        END

        ------------------------------------------------------------
        -- 3️⃣ Drop Indexes (If Exist)
        ------------------------------------------------------------
        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_FunderSettings_FunderId'
              AND object_id = OBJECT_ID('dbo.FunderSettings')
        )
        BEGIN
            DROP INDEX IX_FunderSettings_FunderId ON dbo.FunderSettings;
        END

        IF EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = 'IX_FunderSettings_ClaimFilingIndicatorId'
              AND object_id = OBJECT_ID('dbo.FunderSettings')
        )
        BEGIN
            DROP INDEX IX_FunderSettings_ClaimFilingIndicatorId ON dbo.FunderSettings;
        END

        ------------------------------------------------------------
        -- 4️⃣ Drop Table
        ------------------------------------------------------------
        DROP TABLE dbo.FunderSettings;

    END

    ------------------------------------------------------------
    -- Commit Transaction
    ------------------------------------------------------------
    COMMIT TRANSACTION;

    SELECT 'ROLLBACK COMPLETED: FunderSettings table dropped successfully.' AS Result;

END TRY
BEGIN CATCH

    ------------------------------------------------------------
    -- Rollback If Error
    ------------------------------------------------------------
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;

        SELECT 'ROLLBACK FAILED: Error occurred while dropping FunderSettings table.' AS Result;
    END

    SELECT 'Error occurred: ' + ERROR_MESSAGE() AS ErrorMessage;

    -- Optional:
    -- THROW;

END CATCH;
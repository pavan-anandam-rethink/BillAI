----########################################################
---- Creating FunderSettings Table (Claim Filing Indicator Support)
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    ------------------------------------------------------------
    -- 1️.Create Table If Not Exists
    ------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.tables 
        WHERE name = 'FunderSettings'
    )
    BEGIN
        CREATE TABLE dbo.FunderSettings
        (
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            AccountInfoId INT NOT NULL,
            FunderId INT NOT NULL,
            FunderName VARCHAR(100) NOT NULL,
            ClaimFilingIndicatorId INT NOT NULL,
            IncludeTaxonomyCode BIT NOT NULL DEFAULT (0),

            DateCreated DATETIME2 NOT NULL DEFAULT (GETDATE()),

            DateLastModified DATETIME2 NULL,
            DateDeleted DATETIME2 NULL,

            CreatedBy INT NOT NULL DEFAULT (0),

            ModifiedBy INT NULL,
            DeletedBy INT NULL,

        );

        ------------------------------------------------------------
        -- Foreign Key: ClaimFilingIndicator
        ------------------------------------------------------------
        ALTER TABLE dbo.FunderSettings
        ADD CONSTRAINT FK_FunderSettings_ClaimFilingIndicator
        FOREIGN KEY (ClaimFilingIndicatorId)
        REFERENCES dbo.ClaimFilingIndicator (Id);

        ------------------------------------------------------------
        -- Indexes
        ------------------------------------------------------------
        CREATE INDEX IX_FunderSettings_FunderId
            ON dbo.FunderSettings (FunderId);

        CREATE INDEX IX_FunderSettings_ClaimFilingIndicatorId
            ON dbo.FunderSettings (ClaimFilingIndicatorId);

    END

    ------------------------------------------------------------
    -- Commit Transaction
    ------------------------------------------------------------
    COMMIT TRANSACTION;

    SELECT 'TRANSACTION COMPLETED: FunderSettings table created successfully.' AS Result;

END TRY
BEGIN CATCH

    ------------------------------------------------------------
    -- Rollback If Error
    ------------------------------------------------------------
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;

        SELECT 'ROLLBACK COMPLETED: Error occurred while creating FunderSettings table.' AS Result;
    END

    SELECT 'Error occurred: ' + ERROR_MESSAGE() AS ErrorMessage;

    -- Optional:
    -- THROW;

END CATCH;
------------------------------------------------------------
-- Drop AccountFeatureSettings first (child table)
------------------------------------------------------------
IF OBJECT_ID(N'dbo.AccountFeatureSettings', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AccountFeatureSettings
    DROP CONSTRAINT IF EXISTS FK_AccountFeatureSettings_Features;

    DROP TABLE dbo.AccountFeatureSettings;
END

																	
------------------------------------------------------------
-- Drop unique index on Features
------------------------------------------------------------
IF OBJECT_ID(N'dbo.Features', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'UX_Features_FeatureName'
        AND object_id = OBJECT_ID('dbo.Features')
    )
    BEGIN
        DROP INDEX UX_Features_FeatureName ON dbo.Features;
    END

    DROP TABLE dbo.Features;
END

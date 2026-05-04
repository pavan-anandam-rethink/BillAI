IF EXISTS (
    SELECT 1
    FROM sys.columns c
    WHERE c.name = 'ZipExtension'
      AND c.object_id = OBJECT_ID('dbo.BillingSettingInformation', 'U')
)
BEGIN
    ALTER TABLE dbo.BillingSettingInformation
    DROP COLUMN ZipExtension;
END
IF EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'hcTimezones' 
      AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    DROP TABLE dbo.hcTimezones;
END
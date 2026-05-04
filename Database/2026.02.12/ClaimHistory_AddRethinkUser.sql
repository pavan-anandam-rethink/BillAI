IF NOT EXISTS (
    SELECT 1
    FROM sys.columns 
    WHERE Name = N'RethinkUser'
      AND Object_ID = Object_ID(N'dbo.ClaimHistory')
)
BEGIN
    ALTER TABLE dbo.ClaimHistory
    ADD RethinkUser VARCHAR(150) NULL;
END

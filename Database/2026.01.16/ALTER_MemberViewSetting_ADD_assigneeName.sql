-- Check if the 'assigneeName' column exists in the 'dbo.MemberViewSetting' table
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE name = 'assigneeName'
      AND object_id = OBJECT_ID('dbo.MemberViewSetting')
)
BEGIN
    -- If the column doesn't exist, add it as a 'bit' type and set 'NOT NULL'
    ALTER TABLE dbo.MemberViewSetting
    ADD assigneeName bit NOT NULL  DEFAULT (1);
END
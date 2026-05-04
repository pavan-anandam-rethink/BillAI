IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE name = 'AssigneeId'
      AND object_id = OBJECT_ID('dbo.Claims')
)
BEGIN
    ALTER TABLE dbo.Claims
    ADD AssigneeId INT NOT NULL
        CONSTRAINT DF_Claims_AssigneeId DEFAULT (0);
END

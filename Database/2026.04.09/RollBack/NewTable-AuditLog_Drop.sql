SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NOT NULL
BEGIN
    -- Drop Primary Key constraint if exists
    IF EXISTS (
        SELECT 1 
        FROM sys.key_constraints 
        WHERE name = N'PK_AuditLog_Id'
          AND parent_object_id = OBJECT_ID(N'dbo.AuditLog')
    )
    BEGIN
        ALTER TABLE dbo.AuditLog
        DROP CONSTRAINT PK_AuditLog_Id;
    END

    -- Drop table
    DROP TABLE dbo.AuditLog;
END

COMMIT TRANSACTION;
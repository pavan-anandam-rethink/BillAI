SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Create table only if it does not already exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.AuditLog') AND type = N'U')
BEGIN
    CREATE TABLE dbo.AuditLog
    (
	    Id                       INT IDENTITY(1,1) NOT NULL,
	    EntityId                       INT NOT NULL,
        EntityName               NVARCHAR(max) NOT NULL,
        ActionType               NVARCHAR(1) NOT NULL,
        OldValue                 NVARCHAR(max),
        NewValue                 NVARCHAR(max),
        AccountInfoId            INT NOT NULL,
        ChangedBy                INT NOT NULL,
        ChangedOn                DATETIME2(3) NOT NULL,

       CONSTRAINT PK_AuditLog_Id 
       PRIMARY KEY CLUSTERED (Id)
    );
END
COMMIT TRANSACTION;
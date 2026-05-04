SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.UnProcessedApointmentSchedule', N'U') IS NOT NULL
BEGIN
    -- Drop Primary Key constraint if exists
    IF EXISTS (
        SELECT 1 
        FROM sys.key_constraints 
        WHERE name = N'PK_UnProcessedApointmentSchedule_Id'
          AND parent_object_id = OBJECT_ID(N'dbo.UnProcessedApointmentSchedule')
    )
    BEGIN
        ALTER TABLE dbo.UnProcessedApointmentSchedule
        DROP CONSTRAINT PK_UnProcessedApointmentSchedule_Id;
    END

    -- Drop table
    DROP TABLE dbo.UnProcessedApointmentSchedule;
END

COMMIT TRANSACTION;
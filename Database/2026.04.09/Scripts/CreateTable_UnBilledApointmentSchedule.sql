SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Create table only if it does not already exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.UnProcessedApointmentSchedule') AND type = N'U')
BEGIN
    CREATE TABLE dbo.UnProcessedApointmentSchedule
    (
	    Id                       INT IDENTITY(1,1) NOT NULL,
        AppointmentId            INT NOT NULL,
        FunderId                 INT NOT NULL,
        AccountInfoId            INT NOT NULL,
        ClaimCreationFrequency   NVARCHAR(9) NOT NULL,
        SelectedDays             NVARCHAR(57) NULL,
        Frequency                INT  NULL,
        ExecutionTime            NVARCHAR(5) NULL,
        UtcExecutionDateTime     DATETIME2(3) NULL,
        TimeZone                 NVARCHAR(50) NULL,
        ProcessingStatus         NVARCHAR(11) NOT NULL,
		Retry                    INT NOT NULL,

        -- Audit metadata copied from payload metaData
        CreatedOn            DATETIME2(3) NOT NULL,
        CreatedBy            NVARCHAR(11) NOT NULL,
        ModifiedOn           DATETIME2(3) NULL,
        ModifiedBy           INT NULL,
        DeletedOn            DATETIME2(3) NULL,

       -- Custom Primary Key Name
       CONSTRAINT PK_UnProcessedApointmentSchedule_Id 
       PRIMARY KEY CLUSTERED (Id)
    );
END
COMMIT TRANSACTION;
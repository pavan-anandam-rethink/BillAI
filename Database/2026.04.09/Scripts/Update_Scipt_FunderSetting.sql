/* Billing Schedule – Update FunderSettings Table */
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'ScheduleType' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD ScheduleType int not null
	Default 1;
END
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'ScheduleTime' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD ScheduleTime TIME NULL;
END
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'ScheduleTimeZone' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD ScheduleTimeZone int NULL;
END
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'WeeklyDays' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD WeeklyDays NVARCHAR(200) NULL;
END
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'MonthlyFrequency' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD MonthlyFrequency int NULL;
END
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'NextRunDate' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD NextRunDate DATETIME NULL;
END
 
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = 'CombineChargesForSameClient' 
    AND Object_ID = Object_ID('dbo.FunderSettings')
)
BEGIN
    ALTER TABLE dbo.FunderSettings 
    ADD CombineChargesForSameClient BIT NULL;
END

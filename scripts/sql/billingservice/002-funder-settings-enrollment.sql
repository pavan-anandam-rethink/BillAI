IF COL_LENGTH('dbo.FunderSettings', 'Requires837PEnrollment') IS NULL
BEGIN
    ALTER TABLE dbo.FunderSettings
        ADD Requires837PEnrollment BIT NULL;
END

IF COL_LENGTH('dbo.FunderSettings', 'Is837PEnrollmentCompleted') IS NULL
BEGIN
    ALTER TABLE dbo.FunderSettings
        ADD Is837PEnrollmentCompleted BIT NULL;
END

IF COL_LENGTH('dbo.FunderSettings', 'EnrollmentBillingProviderNpi') IS NULL
BEGIN
    ALTER TABLE dbo.FunderSettings
        ADD EnrollmentBillingProviderNpi NVARCHAR(25) NULL;
END

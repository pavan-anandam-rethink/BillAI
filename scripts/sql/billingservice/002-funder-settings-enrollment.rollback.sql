IF COL_LENGTH('dbo.FunderSettings', 'EnrollmentBillingProviderNpi') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FunderSettings
        DROP COLUMN EnrollmentBillingProviderNpi;
END

IF COL_LENGTH('dbo.FunderSettings', 'Is837PEnrollmentCompleted') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FunderSettings
        DROP COLUMN Is837PEnrollmentCompleted;
END

IF COL_LENGTH('dbo.FunderSettings', 'Requires837PEnrollment') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FunderSettings
        DROP COLUMN Requires837PEnrollment;
END

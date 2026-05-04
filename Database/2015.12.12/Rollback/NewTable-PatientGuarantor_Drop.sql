SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.PatientGuarantor', N'U') IS NOT NULL
BEGIN
    -- Drop FK first if exists
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PatientGuarantor_Invoice' AND parent_object_id = OBJECT_ID(N'dbo.PatientGuarantor'))
    BEGIN
        ALTER TABLE dbo.PatientGuarantor DROP CONSTRAINT FK_PatientGuarantor_Invoice;
    END

    -- Drop index if exists
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PatientGuarantor_InvoiceId' AND object_id = OBJECT_ID(N'dbo.PatientGuarantor'))
    BEGIN
        DROP INDEX UX_PatientGuarantor_InvoiceId ON dbo.PatientGuarantor;
    END

    -- Drop table
    DROP TABLE dbo.PatientGuarantor;
END

COMMIT TRANSACTION;
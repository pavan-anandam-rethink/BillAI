SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Create table only if it does not already exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.PatientGuarantor') AND type = N'U')
BEGIN
    CREATE TABLE dbo.PatientGuarantor
    (
	    Id                   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceId            INT NOT NULL,

        -- IDs from payload
        GuarantorId          INT NULL,              -- contact id
        ClientId             INT NULL,              -- userId
        AccountId            INT NULL,
        MemberId             INT NULL,

        -- Types/flags
        UserType             NVARCHAR(50) NULL,     -- "Client"
        IsPrimaryContact     BIT NULL,
        IsGuarantor          BIT NULL,

        -- Name
        FirstName            NVARCHAR(150) NULL,
        MiddleName           NVARCHAR(150) NULL,
        LastName             NVARCHAR(150) NULL,
        Prefix               NVARCHAR(50)  NULL,
        Suffix               NVARCHAR(50)  NULL,

        -- Contact
        Email                NVARCHAR(250) NULL,
        Phone                NVARCHAR(50)  NULL,

        -- Relationship
        RelationToClient      NVARCHAR(100) NULL,
        RelationshipToInsured INT NULL,

        -- Demographics
        GenderId             INT NULL,
        MaritalStatusId      INT NULL,
        DateOfBirth          DATE NULL,
        TimezoneId           INT NULL,

        -- Insurance/records
        MedicalRecordNumber   NVARCHAR(100) NULL,
        InsurancePolicyNumber NVARCHAR(100) NULL,
        HasSystemLogin        BIT NULL,

        -- Address snapshot
        AddressId            INT NULL,
        Street1              NVARCHAR(300) NULL,
        Street2              NVARCHAR(300) NULL,
        City                 NVARCHAR(150) NULL,
        StateId              INT NULL,
        State                NVARCHAR(100) NULL,
        ZipCode              NVARCHAR(20)  NULL,
        CountryId            INT NULL,
        Country              NVARCHAR(100) NULL,
        Town                 NVARCHAR(100) NULL,

        -- Audit metadata copied from payload metaData
        CreatedOn            DATETIME2(3) NULL,
        CreatedBy            INT NULL,
        ModifiedOn           DATETIME2(3) NULL,
        ModifiedBy           INT NULL,
        DeletedOn            DATETIME2(3) NULL,
        DeletedBy            INT NULL,
        UtcCreatedOn         DATETIME2(3) NULL,
        UtcModifiedOn        DATETIME2(3) NULL,
        UtcDeletedOn         DATETIME2(3) NULL
    );
END

-- Unique index: one snapshot per invoice
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PatientGuarantor_InvoiceId' AND object_id = OBJECT_ID(N'dbo.PatientGuarantor'))
BEGIN
    CREATE UNIQUE INDEX UX_PatientGuarantor_InvoiceId ON dbo.PatientGuarantor(InvoiceId);
END

-- Optional FK if PatientInvoice table exists (uncomment if present)
 IF OBJECT_ID(N'dbo.PatientInvoice', N'U') IS NOT NULL
 BEGIN
     IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PatientGuarantor_Invoice')
     BEGIN
         ALTER TABLE dbo.PatientGuarantor
         ADD CONSTRAINT FK_PatientGuarantor_Invoice
         FOREIGN KEY (InvoiceId) REFERENCES dbo.PatientInvoice(Id);
     END
 END

COMMIT TRANSACTION;
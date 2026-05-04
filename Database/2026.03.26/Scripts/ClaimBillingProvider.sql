IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.name = 'ClaimBillingProvider'
)
BEGIN

CREATE TABLE ClaimBillingProvider
(
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

    ClaimId INT NOT NULL,

    ProviderType VARCHAR(10) NOT NULL, -- Entity / Person

    FirstName NVARCHAR(100) NULL,

    LastNameOrFacilityName NVARCHAR(200) NOT NULL,

    NPI VARCHAR(10) NOT NULL,

    TaxId VARCHAR(20) NULL,

    TaxonomyCode VARCHAR(20) NULL,

    AddressLine1 NVARCHAR(200) NOT NULL,

    AddressLine2 NVARCHAR(200) NULL,

    City NVARCHAR(100) NOT NULL,

    State VARCHAR(2) NOT NULL,

    Zip VARCHAR(5) NOT NULL,

    ZipExt VARCHAR(4) NOT NULL,

    DateCreated DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CreatedBy INT NOT NULL,

    DateLastModified DATETIME2(7) NULL,

    ModifiedBy INT NULL,

    DateDeleted DATETIME2(7) NULL,

    DeletedBy INT NULL
)

ALTER TABLE ClaimBillingProvider
ADD CONSTRAINT FK_ClaimBillingProvider_Claim
FOREIGN KEY (ClaimId)
REFERENCES Claims(Id)

CREATE INDEX IX_ClaimBillingProvider_ClaimId
ON ClaimBillingProvider (ClaimId)

END
GO






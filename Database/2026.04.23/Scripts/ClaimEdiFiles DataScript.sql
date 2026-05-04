
--DATASCRIPT

BEGIN TRANSACTION;

------------------------------------------------------------
-- 1. Create table only if not exists
------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 
    FROM sys.objects 
    WHERE object_id = OBJECT_ID(N'[dbo].[ClaimEdiFilesPath]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[ClaimEdiFilesPath] (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        AccountInfoId INT NULL,
        FileType VARCHAR(10) NOT NULL 
            CONSTRAINT CK_ClaimEdiFilesPath_FileType 
            CHECK (FileType IN ('837', '999', '277', '835', '270', '271')),

        ClaimSubmissionId INT NULL,             -- 837, 999, 277, 835 (rebill support)
        ClaimId INT NULL,                       -- 999, 277, 835
        PaymentId INT NULL,                     -- 835 only

        BlobFilePath NVARCHAR(500) NOT NULL,

        [DateCreated] [datetime2](7) NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [DateLastModified] [datetime2](7) NULL,
        [ModifiedBy] [int] NULL,
        [DateDeleted] [datetime2](7) NULL,
        [DeletedBy] [int] NULL,
    );
END;

------------------------------------------------------------
-- 2. Add Foreign Keys
------------------------------------------------------------

-- FK to ClaimSubmissions table
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_ClaimEdiFilesPath_ClaimSubmissions'
)
BEGIN
    ALTER TABLE [dbo].[ClaimEdiFilesPath]
    ADD CONSTRAINT FK_ClaimEdiFilesPath_ClaimSubmissions
    FOREIGN KEY (ClaimSubmissionId) REFERENCES [dbo].[ClaimSubmissions](Id);
END;

------------------------------------------------------------
-- 3. Add Indexes (Optimized for all file types)
------------------------------------------------------------

-- Index for AccountInfoId
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_ClaimEdiFilesPath_AccountInfoId_ClaimId'
      AND object_id = OBJECT_ID(N'[dbo].[ClaimEdiFilesPath]')
)
BEGIN
    CREATE NONCLUSTERED INDEX index_name
        ON ClaimEdiFilesPath (AccountInfoId, ClaimId)
END;

COMMIT TRANSACTION;

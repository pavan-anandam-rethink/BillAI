
-- ROLLBACK SCRIPT for ClaimEdiFilesPath table creation

BEGIN TRANSACTION;

------------------------------------------------------------
-- 1. Drop Indexes
------------------------------------------------------------
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_ClaimEdiFilesPath_AccountInfoId_ClaimId'
      AND object_id = OBJECT_ID(N'[dbo].[ClaimEdiFilesPath]')
)
BEGIN
    DROP INDEX [IX_ClaimEdiFilesPath_AccountInfoId_ClaimId] 
        ON [dbo].[ClaimEdiFilesPath];
    PRINT 'Index IX_ClaimEdiFilesPath_AccountInfoId_ClaimId dropped successfully.';
END;

------------------------------------------------------------
-- 2. Drop Foreign Keys
------------------------------------------------------------
IF EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_ClaimEdiFilesPath_ClaimSubmissions'
      AND parent_object_id = OBJECT_ID(N'[dbo].[ClaimEdiFilesPath]')
)
BEGIN
    ALTER TABLE [dbo].[ClaimEdiFilesPath]
    DROP CONSTRAINT [FK_ClaimEdiFilesPath_ClaimSubmissions];
    PRINT 'Foreign key FK_ClaimEdiFilesPath_ClaimSubmissions dropped successfully.';
END;

------------------------------------------------------------
-- 3. Drop Table
------------------------------------------------------------
IF EXISTS (
    SELECT 1 
    FROM sys.objects 
    WHERE object_id = OBJECT_ID(N'[dbo].[ClaimEdiFilesPath]')
      AND type = 'U'
)
BEGIN
    DROP TABLE [dbo].[ClaimEdiFilesPath];
    PRINT 'Table ClaimEdiFilesPath dropped successfully.';
END;

COMMIT TRANSACTION;


--Rollback


BEGIN TRANSACTION

-- Drop EntityIdentifierCode column if it exists
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N'EntityIdentifierCode'
      AND Object_ID = Object_ID(N'[dbo].[EraValidationErrors]')
)
BEGIN
    ALTER TABLE [dbo].[EraValidationErrors]
    DROP COLUMN [EntityIdentifierCode]
    
    PRINT 'EntityIdentifierCode column dropped'
END

-- Drop StcPosition column if it exists
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N'StcPosition'
      AND Object_ID = Object_ID(N'[dbo].[EraValidationErrors]')
)
BEGIN
    ALTER TABLE [dbo].[EraValidationErrors]
    DROP COLUMN [StcPosition]
    
    PRINT 'StcPosition column dropped'
END

COMMIT TRANSACTION
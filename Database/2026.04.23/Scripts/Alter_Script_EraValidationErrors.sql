
--Data Script

BEGIN TRANSACTION

-- Add EntityIdentifierCode column if it doesn't exist
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N'EntityIdentifierCode'
      AND Object_ID = Object_ID(N'[dbo].[EraValidationErrors]')
)
BEGIN
    ALTER TABLE [dbo].[EraValidationErrors]
    ADD [EntityIdentifierCode] NVARCHAR(50) NULL
    
    PRINT 'EntityIdentifierCode column added'
END

-- Add StcPosition column if it doesn't exist
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N'StcPosition'
      AND Object_ID = Object_ID(N'[dbo].[EraValidationErrors]')
)
BEGIN
    ALTER TABLE [dbo].[EraValidationErrors]
    ADD [StcPosition] NVARCHAR(50) NULL
    
    PRINT 'StcPosition column added'
END

COMMIT TRANSACTION




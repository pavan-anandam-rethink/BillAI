-- Rollback Script: Drop State table if it exists
IF OBJECT_ID('dbo.State', 'U') IS NOT NULL
BEGIN
    -- Drop the table (automatically drops PK, defaults, and indexes)
    DROP TABLE dbo.State;
    PRINT 'Rollback: State table has been dropped.';
END
ELSE
BEGIN
    PRINT 'Rollback: State table does not exist.';
END
GO
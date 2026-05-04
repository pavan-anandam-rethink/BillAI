
BEGIN TRY
    BEGIN TRANSACTION;

    --==============================================================
    -- 1. Check current datatype
    --==============================================================
    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('[Reporting].[PaymentsAdjustments]')
          AND c.name = 'remarkCode'
          AND t.name = 'int'
    )
    BEGIN
        PRINT 'Column is INT. Altering to VARCHAR(10)...';

        --==============================================================
        -- 2. Alter column type
        --==============================================================
        ALTER TABLE [Reporting].[PaymentsAdjustments]
            ALTER COLUMN remarkCode VARCHAR(10);

        PRINT 'Column successfully altered to VARCHAR(10).';
    END
    ELSE
    BEGIN
        PRINT 'Column is already VARCHAR or a different type. No change applied.';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    PRINT 'Error occurred. Rolling back...';
    ROLLBACK TRANSACTION;

    -- Return detailed error info
    THROW;
END CATCH;

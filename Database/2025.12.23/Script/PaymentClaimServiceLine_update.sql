----########################################################
---- Alter columns in [dbo].[PaymentClaimServiceLine]
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /* ===============================
       serviceCode → NVARCHAR(50)
       =============================== */
    IF EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'PaymentClaimServiceLine'
          AND COLUMN_NAME = 'serviceCode'
          AND (DATA_TYPE <> 'nvarchar' OR CHARACTER_MAXIMUM_LENGTH < 50)
    )
    BEGIN
        ALTER TABLE dbo.PaymentClaimServiceLine
        ALTER COLUMN serviceCode NVARCHAR(50);

        PRINT 'Column [serviceCode] altered to NVARCHAR(50).';
    END
    ELSE
        PRINT 'Column [serviceCode] already NVARCHAR(50) or larger.';

    /* ===============================
       serviceCodeOrig → NVARCHAR(50)
       =============================== */
    IF EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo'
          AND TABLE_NAME = 'PaymentClaimServiceLine'
          AND COLUMN_NAME = 'serviceCodeOrig'
          AND (DATA_TYPE <> 'nvarchar' OR CHARACTER_MAXIMUM_LENGTH < 50)
    )
    BEGIN
        ALTER TABLE dbo.PaymentClaimServiceLine
        ALTER COLUMN serviceCodeOrig NVARCHAR(50);

        PRINT 'Column [serviceCodeOrig] altered to NVARCHAR(50).';
    END
    ELSE
        PRINT 'Column [serviceCodeOrig] already NVARCHAR(50) or larger.';

    /* ===============================
       Date columns → DATETIME2(7)
       =============================== */
    DECLARE @DateColumns TABLE (ColumnName SYSNAME);

    INSERT INTO @DateColumns (ColumnName)
    VALUES
        ('DateOfService'),
        ('DateOfServiceOrig'),
        ('ServiceStartDate'),
        ('ServiceStartDateOrig'),
        ('ServiceEndDate'),
        ('ServiceEndDateOrig');

    DECLARE @ColumnName SYSNAME;
    DECLARE date_cursor CURSOR FOR
        SELECT ColumnName FROM @DateColumns;

    OPEN date_cursor;
    FETCH NEXT FROM date_cursor INTO @ColumnName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = 'PaymentClaimServiceLine'
              AND COLUMN_NAME = @ColumnName
              AND DATA_TYPE <> 'datetime2'
        )
        BEGIN
            DECLARE @sql NVARCHAR(MAX);
            SET @sql = 'ALTER TABLE dbo.PaymentClaimServiceLine
                    ALTER COLUMN ' + QUOTENAME(@ColumnName) + ' DATETIME2(7) NULL;';

            EXEC sp_executesql @sql;

            PRINT 'Column [' + @ColumnName + '] altered to DATETIME2(7).';
        END
        ELSE
            PRINT 'Column [' + @ColumnName + '] already DATETIME2.';

        FETCH NEXT FROM date_cursor INTO @ColumnName;
    END

    CLOSE date_cursor;
    DEALLOCATE date_cursor;

    COMMIT TRANSACTION;
    PRINT 'TRANSACTION COMPLETED SUCCESSFULLY';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'ROLLBACK COMPLETED';
    END

    PRINT 'Error occurred: ' + ERROR_MESSAGE();
END CATCH;

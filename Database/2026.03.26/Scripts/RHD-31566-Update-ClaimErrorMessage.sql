-- Created by GitHub Copilot in SSMS - review carefully before executing

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    -- ====================================
    -- Preview: What will be changed
    -- ====================================
    SELECT id, longDescription AS CurrentValue
    FROM dbo.ClaimErrorMessages
    WHERE id IN (88, 90)
    ORDER BY id;

    -- ====================================
    -- Execute Updates in Transaction
    -- ====================================
    BEGIN TRANSACTION;

    -- Update ID 88
    UPDATE dbo.ClaimErrorMessages
    SET longDescription = 'Insured ID must be alphanumeric & Length must be >=2 and <=80'
    WHERE id = 88;

    -- Update ID 90
    UPDATE dbo.ClaimErrorMessages
    SET longDescription = 'Insured ID must be alphanumeric & Length must be >=2 and <=80'
    WHERE id = 90;

    -- ====================================
    -- Verify: Confirm the changes
    -- ====================================
    SELECT id, longDescription, GETDATE() AS UpdatedAt
    FROM dbo.ClaimErrorMessages
    WHERE id IN (88, 90)
    ORDER BY id;

    COMMIT TRANSACTION;

    PRINT 'Update completed successfully. 2 row(s) updated.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH;
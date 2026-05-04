
/* ============================================================
   RollBack the changes
   ============================================================ */

-- Check if the backup table exists
IF OBJECT_ID('dbo.RHD_2011_Claims', 'U') IS NULL
BEGIN
    PRINT 'Backup table RHD_2011_Claims does not exist. Rollback cannot be performed.';
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- Restore original statuses from RHD_2011_Claims
    UPDATE c
    SET c.hcClaimStatusId = r.HcClaimStatusId
    FROM dbo.Claims c
    INNER JOIN dbo.RHD_2011_Claims r
        ON c.Id = r.Id
    WHERE r.BalanceAmount = 0;

    PRINT 'Rollback complete: Claims restored to their original statuses.';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR('Rollback failed: %s', 16, 1, @Err);
END CATCH;

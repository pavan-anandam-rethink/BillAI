
----########################################################
---- ROLLBACK SCRIPT
---- Delete Claim Error Messages for Clearing House Errors
---- 3210 - Authentication failure
---- 3211 - Connection issue
---- 3212 - Upload failed
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    DELETE FROM dbo.ClaimErrorMessages
    WHERE errorNumber IN (3210, 3211, 3212);

    COMMIT TRANSACTION;

    SELECT 
        'ROLLBACK COMPLETED: ClaimErrorMessages 3210, 3211, 3212 deleted' 
        AS Result,
        @@ROWCOUNT AS RowsDeleted;

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT 
        'ROLLBACK FAILED' AS Result,
        ERROR_MESSAGE() AS ErrorMessage;

END CATCH;
GO
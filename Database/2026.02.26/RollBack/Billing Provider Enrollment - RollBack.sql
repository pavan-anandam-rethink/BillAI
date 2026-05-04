----########################################################
---- Rollback: Remove Claim Error Message (3209)
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM dbo.ClaimErrorMessages
        WHERE errorNumber = 3209
    )
    BEGIN
        DELETE FROM dbo.ClaimErrorMessages
        WHERE errorNumber = 3209;
    END

    COMMIT TRANSACTION;

    SELECT 
        'ROLLBACK COMPLETED: ClaimErrorMessages 3209 removed (if existed)' 
        AS Result;

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT 
        'ROLLBACK FAILED' AS Result,
        ERROR_MESSAGE() AS ErrorMessage;

END CATCH;

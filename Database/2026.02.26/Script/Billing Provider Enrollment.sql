----########################################################
---- Insert Claim Error Message (3209) - Billing Provider Enrollment Missing
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ClaimErrorMessages
        WHERE errorNumber = 3209
    )
    BEGIN
        INSERT INTO dbo.ClaimErrorMessages
        (
            errorNumber,
            shortDescription,
            longDescription,
            severity,
            claimErrorCategoryId,
            dateCreated,
            dateLastModified,
            dateDeleted,
            CreatedBy,
            ModifiedBy,
            DeletedBy
        )
        VALUES
        (
            3209,
            'Billing Provider - Enrollment Missing',
            'Billing Provider - Enrollment Missing',
            2,
            6,
            GETDATE(),
            GETDATE(),
            NULL,
            0,
            NULL,
            NULL
        );
    END

    COMMIT TRANSACTION;

    SELECT 
        'DATA INSERT COMPLETED: ClaimErrorMessages 3209 inserted (if not exists)' 
        AS Result;

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT 
        'DATA INSERT FAILED' AS Result,
        ERROR_MESSAGE() AS ErrorMessage;

END CATCH;
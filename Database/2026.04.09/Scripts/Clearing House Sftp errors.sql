----########################################################
---- Insert Claim Error Messages for Clearing House Errors
---- 3210 - Authentication failure
---- 3211 - Connection issue
---- 3212 - Upload failed
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    -- 3210: Clearing House - Authentication failure
    IF NOT EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE errorNumber = 3210)
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
            3210,
            'Clearing House - Authentication failure',
            'Clearing House - Authentication failure',
            2,
            7,
            GETDATE(),
            GETDATE(),
            NULL,
            0,
            NULL,
            NULL
        );
    END

    -- 3211: Clearing House - Connection issue
     IF NOT EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE errorNumber = 3211)      
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
            3211,
            'Clearing House - Connection issue',
            'Clearing House - Connection issue',
            2,
            7,
            GETDATE(),
            GETDATE(),
            NULL,
            0,
            NULL,
            NULL
        );
    END

    -- 3212: Clearing House - Upload failed
  IF NOT EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE errorNumber = 3212)      
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
            3212,
            'Clearing House - Upload failed',
            'Clearing House - Upload failed',
            2,
            7,
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
        'DATA INSERT COMPLETED: ClaimErrorMessages 3210, 3211, 3212 inserted (if not exists)' 
        AS Result;

END TRY
BEGIN CATCH

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    SELECT 
        'DATA INSERT FAILED' AS Result,
        ERROR_MESSAGE() AS ErrorMessage;

END CATCH;
----########################################################
------ Claim Error Messages Insert
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;

        -- Check if a record with the same errorNumber already exists
        IF NOT EXISTS (
            SELECT 1
            FROM ClaimErrorMessages
            WHERE errorNumber = 3208
        )
        BEGIN
            -- Inserting new record into ClaimErrorMessages
            INSERT INTO ClaimErrorMessages
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
                3208, 
                'Insured ID Incorrect Length', 
                'Insured ID must be alphanumeric & Length must be >=2 and <=12', 
                2, 
                1, 
                CURRENT_TIMESTAMP, 
                CURRENT_TIMESTAMP, 
                NULL, 
                0, 
                NULL, 
                NULL
            );

            -- Optionally, you can return a message indicating the record was inserted
            SELECT 'Record inserted successfully';
        END
        ELSE
        BEGIN
            -- Optionally, return a message indicating the record already exists
            SELECT 'Record with errorNumber 3208 already exists';
        END

    COMMIT TRANSACTION;
    SELECT 'TRANSACTION COMPLETED';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT 'ROLLBACK COMPLETED';
    END
    SELECT 'Error occurred: ' + ERROR_MESSAGE();
END CATCH;

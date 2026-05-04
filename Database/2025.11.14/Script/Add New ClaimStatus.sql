
----########################################################
------ Add new Claim Status for 'Submission Failed'
----########################################################
BEGIN TRY
    BEGIN TRANSACTION;
        -- Check if a record with the same ClaimStatus already exists
        IF NOT EXISTS (
            SELECT 1
            FROM ClaimStatus
            WHERE ID = 19 and Name = 'Submission Failed'
        )
        BEGIN
            -- Inserting new record into ClaimStatus
            INSERT INTO ClaimStatus
            (
                Id, 
                Name, 
                dateCreated, 
                dateLastModified, 
                dateDeleted
            )
            VALUES
            (
                19, 
                'Submission Failed', 
                CURRENT_TIMESTAMP, 
                CURRENT_TIMESTAMP, 
                NULL
            );
            SELECT 'Record inserted successfully';
        END
        ELSE
        BEGIN
            SELECT 'Record with Id 19 already exists';
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
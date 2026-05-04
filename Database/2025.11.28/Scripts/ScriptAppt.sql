----########################################################
------ Creating AppointmentClaimProcessingError Table with Transaction and Index on Foreign Key
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

        -- Check if the table exists before creating it
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppointmentClaimProcessingError')
        BEGIN
            -- Create the AppointmentClaimProcessingError table
            CREATE TABLE AppointmentClaimProcessingError (
                id INT PRIMARY KEY IDENTITY,                   -- Auto increment primary key
                claimAppointmentLinkId  INT,                             -- Foreign key to ClaimAppointmentLink
                errorMessage NVARCHAR(1000) NOT NULL,          -- Error message (required)
                createdBy INT,                                 -- Created by (now INT as per your entity)
                dateCreated DATETIME DEFAULT GETDATE(),        -- Date created (default to current date)
                modifiedBy INT NULL,                                -- Modified by (INT as per your entity)
                dateLastModified DATETIME NULL,                         -- Date modified (nullable)
                dateDeleted DATETIME NULL,                     -- Date deleted (nullable)
                deletedBy INT NULL,                            -- Deleted by (nullable, INT)

                -- Foreign key constraint to ClaimAppointmentLink
                CONSTRAINT FK_AppointmentClaimProcessingErrors_ClaimAppointmentLink 
                    FOREIGN KEY (ClaimAppointmentLinkId)
                    REFERENCES ClaimAppointmentLink(Id)
            );

            -- Add an index on the foreign key column to improve query performance
            CREATE NONCLUSTERED INDEX IX_AppointmentClaimProcessingError_ClaimAppointmentLinkId
                ON AppointmentClaimProcessingError (ClaimAppointmentLinkId);

        END

    -- Commit the transaction if table creation is successful
    COMMIT TRANSACTION;
    SELECT 'TRANSACTION COMPLETED: Table created successfully and index added' AS Result;

END TRY
BEGIN CATCH
    -- If an error occurs, rollback the transaction
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT 'ROLLBACK COMPLETED: Error occurred while creating the table' AS Result;
    END

    -- Display the error message
    SELECT 'Error occurred: ' + ERROR_MESSAGE() AS ErrorMessage;
    -- Optionally, re-throw the error (useful for debugging)
    -- THROW;
END CATCH;

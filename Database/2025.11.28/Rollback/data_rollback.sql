SET NOEXEC OFF
SET NOCOUNT ON
SET NUMERIC_ROUNDABORT OFF
GO

SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO

SET XACT_ABORT ON
GO

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE

BEGIN TRANSACTION

IF @@ERROR <> 0 SET NOEXEC ON

-- Start the rollback logic for table creation
BEGIN TRY
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

    -- Commit the transaction if table creation is successful
    COMMIT TRANSACTION;
    PRINT 'TRANSACTION COMPLETED: Table created successfully';

END TRY
BEGIN CATCH
    -- If an error occurs, rollback the transaction
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'ROLLBACK COMPLETED: Error occurred while creating the table';
    END

    -- Display the error message
    PRINT 'Error occurred: ' + ERROR_MESSAGE();

    -- Optionally, re-throw the error for further debugging or logging
    -- THROW;
END CATCH;

GO

-- Final check for transaction completion
IF @@TRANCOUNT > 0 
    COMMIT TRANSACTION;  -- Ensure any uncommitted transactions are committed

GO

DECLARE @Success AS BIT
SET @Success = 1

-- Check success of the transaction and print a status message
IF (@Success = 1)
    PRINT 'Database updation succeeded'
ELSE
    BEGIN
        IF @@TRANCOUNT > 0 
            ROLLBACK TRANSACTION
        PRINT 'The database updation failed. ROLLBACK TRANSACTION'
    END

GO

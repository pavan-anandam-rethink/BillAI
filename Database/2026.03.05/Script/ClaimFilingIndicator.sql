----########################################################
---- Creating ClaimFilingIndicator Master Table & Seeding Data
----########################################################

BEGIN TRY
    BEGIN TRANSACTION;

    ------------------------------------------------------------
    -- 1️⃣ Create Table If Not Exists
    ------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.tables 
        WHERE name = 'ClaimFilingIndicator'
    )
    BEGIN
        CREATE TABLE dbo.ClaimFilingIndicator
        (
            Id INT IDENTITY(1,1) PRIMARY KEY,

            Code VARCHAR(2) NOT NULL,
            Description VARCHAR(150) NOT NULL,

            DateCreated DATETIME NOT NULL DEFAULT (GETDATE()),
            DateLastModified DATETIME NOT NULL DEFAULT (GETDATE()),
            DateDeleted DATETIME NULL,

            CreatedBy INT NOT NULL DEFAULT (0),
            ModifiedBy INT NULL,
            DeletedBy INT NULL
        );

   
    END

    ------------------------------------------------------------
    -- 2️⃣ Insert Master Data (Only If Not Already Inserted)
    ------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM dbo.ClaimFilingIndicator)
    BEGIN
        INSERT INTO dbo.ClaimFilingIndicator (Code, Description)
        VALUES
        ('ZZ', 'Default'),
        ('11', 'Other Non-Federal Programs'),
        ('12', 'Preferred Provider Organization (PPO)'),
        ('13', 'Point of Service (POS)'),
        ('14', 'Exclusive Provider Organization (EPO)'),
        ('15', 'Indemnity Insurance'),
        ('16', 'Health Maintenance Organization (HMO)'),
        ('BL', 'Blue Cross/Blue Shield'),
        ('CH', 'Champus'),
        ('CI', 'Commercial Insurance Co.'),
        ('DS', 'Disability'),
        ('FI', 'Federal Employees Program'),
        ('HM', 'Health Maintenance Organization'),
        ('LM', 'Liability Medical'),
        ('MA', 'Medicare Part A'),
        ('MB', 'Medicare Part B'),
        ('MC', 'Medicaid'),
        ('OF', 'Other Federal Program'),
        ('TV', 'Title V'),
        ('VA', 'Veterans Affairs Plan'),
        ('WC', 'Workers’ Compensation Health Claim');
    END

    ------------------------------------------------------------
    -- Commit Transaction
    ------------------------------------------------------------
    COMMIT TRANSACTION;

    SELECT 'TRANSACTION COMPLETED: ClaimFilingIndicator table created and master data inserted successfully.' AS Result;

END TRY
BEGIN CATCH

    ------------------------------------------------------------
    -- Rollback If Error
    ------------------------------------------------------------
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;

        SELECT 'ROLLBACK COMPLETED: Error occurred while creating ClaimFilingIndicator table or inserting data.' AS Result;
    END

    SELECT 'Error occurred: ' + ERROR_MESSAGE() AS ErrorMessage;

    -- Optional:
    -- THROW;

END CATCH;
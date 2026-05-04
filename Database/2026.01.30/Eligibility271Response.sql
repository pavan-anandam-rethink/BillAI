SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.objects
        WHERE object_id = OBJECT_ID(N'dbo.Eligibility271Response')
          AND type = N'U'
    )
    BEGIN
        CREATE TABLE dbo.Eligibility271Response
        (
            Eligibility271ResponseId BIGINT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_Eligibility271Response PRIMARY KEY,

            TransactionControlNumber UNIQUEIDENTIFIER NOT NULL,       
		    FunderId INT NOT NULL,
            AccountId INT NOT NULL,
		    EffectiveStartDate DATE NULL,
            EffectiveEndDate DATE NULL,
            CoverageStatus VARCHAR(50) NULL,
            CreatedBy INT NOT NULL,
            CreatedDate DATETIME2(3) NOT NULL
                CONSTRAINT DF_Eligibility271Response_CreatedDate
                DEFAULT (SYSUTCDATETIME()),
            ModifiedBy INT NULL,
            ModifiedDate DATETIME2(3) NULL,		
		    SubscriberStartDate DATE NULL,
            SubscriberEndDate DATE NULL,
            PlanStartDate DATE NULL,
            PlanEndDate DATE NULL
        );
    END

    -- Index for searching by transaction control number
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Eligibility271Response_TransactionControlNumber'
          AND object_id = OBJECT_ID(N'dbo.Eligibility271Response')
    )
    BEGIN
       CREATE NONCLUSTERED INDEX IX_Eligibility271Response_Funder_Account_CreatedBy
        ON dbo.Eligibility271Response
        (
            FunderId,
            AccountId,
            CreatedBy
        );
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

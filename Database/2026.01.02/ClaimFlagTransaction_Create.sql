SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Create table only if it does not already exist
IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.ClaimFlagTransaction')
      AND type = N'U'
)
BEGIN
    CREATE TABLE dbo.ClaimFlagTransaction
    (
        id int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ClaimFlagTransaction PRIMARY KEY,

        accountInfoId INT NOT NULL,

        hcClaimId INT NOT NULL
            CONSTRAINT FK_ClaimFlagTransaction_Claim
                FOREIGN KEY REFERENCES dbo.Claims(id),

        reasonId INT NOT NULL
            CONSTRAINT FK_ClaimFlagTransaction_Reason
                FOREIGN KEY REFERENCES dbo.ClaimFlagReasonMaster(id),

        comment NVARCHAR(MAX) NULL,

        actionType NVARCHAR(50) NOT NULL,

        createdBy INT NOT NULL,
        dateCreated DATETIME2(7) NOT NULL,
        modifiedBy INT NULL,
        dateLastModified DATETIME2(7) NULL,
        dateDeleted DATETIME2(7) NULL ,               -- Soft delete indicator

        CONSTRAINT CK_ClaimFlagTransaction_ActionType
            CHECK (actionType IN ('Flagged', 'Unflagged', 'Updated'))
    );
END

-- Index for claim-level history lookups
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ClaimFlagTransaction_HcClaimId'
      AND object_id = OBJECT_ID(N'dbo.ClaimFlagTransaction')
)
BEGIN
    CREATE INDEX IX_ClaimFlagTransaction_HcClaimId
    ON dbo.ClaimFlagTransaction (hcClaimId)
    INCLUDE (reasonId, actionType, dateCreated);
END

-- Index for account-level auditing
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ClaimFlagTransaction_AccountInfoId'
      AND object_id = OBJECT_ID(N'dbo.ClaimFlagTransaction')
)
BEGIN
    CREATE INDEX IX_ClaimFlagTransaction_AccountInfoId
    ON dbo.ClaimFlagTransaction (accountInfoId);
END

-- Index for chronological audit queries
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ClaimFlagTransaction_CreatedDate'
      AND object_id = OBJECT_ID(N'dbo.ClaimFlagTransaction')
)
BEGIN
    CREATE INDEX IX_ClaimFlagTransaction_CreatedDate
    ON dbo.ClaimFlagTransaction (dateCreated DESC);
END

-- Prevent UPDATE and DELETE (append-only enforcement)
IF NOT EXISTS (
    SELECT 1
    FROM sys.triggers
    WHERE name = N'TR_ClaimFlagTransaction_NoUpdateDelete'
)
BEGIN
    EXEC ('
        CREATE TRIGGER TR_ClaimFlagTransaction_NoUpdateDelete
        ON dbo.ClaimFlagTransaction
        INSTEAD OF DELETE
        AS
        BEGIN
            THROW 50001, ''ClaimFlagTransaction is append-only. deletes are not allowed.'', 1;
        END
    ');
END

------------------------------------------------------------
-- Seed master data (system default: accountInfoId = 0)
------------------------------------------------------------
INSERT INTO dbo.ClaimFlagReasonMaster
    (reasonName, reasonDescription, accountInfoId, createdBy, dateCreated)
SELECT v.reasonName, NULL, 0, 18421, SYSUTCDATETIME()
FROM (VALUES
    (N'Authorization'),
    (N'Appointment'),
    (N'Documentation'),
    (N'Billing Provider'),
    (N'Rendering Provider'),
    (N'Referring Provider'),
    (N'Credentialing/Enrollment'),
    (N'Payer'),
    (N'Coding'),
    (N'Eligibility'),
    (N'Other')
) v(reasonName)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ClaimFlagReasonMaster c
    WHERE c.reasonName = v.reasonName
      AND c.accountInfoId = 0
      AND c.dateDeleted IS NULL
);

COMMIT TRANSACTION;

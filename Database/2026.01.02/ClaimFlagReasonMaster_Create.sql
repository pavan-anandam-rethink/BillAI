SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Create table only if it does not already exist
IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.ClaimFlagReasonMaster')
      AND type = N'U'
)
BEGIN
    CREATE TABLE dbo.ClaimFlagReasonMaster
    (
        id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ClaimFlagReasonMaster PRIMARY KEY,

        reasonName NVARCHAR(100) NOT NULL,          -- Display name shown in UI
        reasonDescription NVARCHAR(255) NULL,       -- Optional description / tooltip

        accountInfoId INT NOT NULL
            CONSTRAINT DF_ClaimFlagReasonMaster_AccountInfoId DEFAULT (0), -- 0 = system default

        createdBy INT NOT NULL,
        dateCreated DATETIME2(7) NOT NULL,
        modifiedBy INT NULL,
        dateLastModified DATETIME2(7) NULL,
        dateDeleted DATETIME2(7) NULL                -- Soft delete indicator
    );
END

-- Unique index: prevent duplicate reasons per account (active only)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_ClaimFlagReasonMaster_ReasonName_Account'
      AND object_id = OBJECT_ID(N'dbo.ClaimFlagReasonMaster')
)
BEGIN
    CREATE UNIQUE INDEX UX_ClaimFlagReasonMaster_ReasonName_Account
    ON dbo.ClaimFlagReasonMaster (ReasonName, AccountInfoId)
    WHERE DateDeleted IS NULL;
END

-- Helpful index for filtering active/system vs account-specific records
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ClaimFlagReasonMaster_Active'
      AND object_id = OBJECT_ID(N'dbo.ClaimFlagReasonMaster')
)
BEGIN
    CREATE INDEX IX_ClaimFlagReasonMaster_Active
    ON dbo.ClaimFlagReasonMaster (AccountInfoId)
    WHERE DateDeleted IS NULL;
END

COMMIT TRANSACTION;

------------------------------------------------------------
-- 1) FEATURES (Master)
------------------------------------------------------------
IF OBJECT_ID(N'dbo.Features', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Features
    (
        id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Features PRIMARY KEY CLUSTERED,

        FeatureName  NVARCHAR(200) NOT NULL,

        [createdBy] [int] NOT NULL,
		[dateCreated] [datetime] NOT NULL,
		[modifiedBy] [int] NULL,
		[dateLastModified] [datetime] NULL,
		[dateDeleted] [datetime] NULL,
		[DeletedBy] [int] NULL,
    );

    -- Unique FeatureName
    CREATE UNIQUE NONCLUSTERED INDEX UX_Features_FeatureName
        ON dbo.Features (FeatureName);
END
GO
------------------------------------------------------------
-- 2) ACCOUNT FEATURE SETTINGS (Transaction)
------------------------------------------------------------
IF OBJECT_ID(N'dbo.AccountFeatureSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountFeatureSettings
    (
        id  INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AccountFeatureSettings PRIMARY KEY CLUSTERED,

        FeatureId    INT NOT NULL,

        AccountId    INT NOT NULL,       

        Status       BIT NOT NULL
            CONSTRAINT DF_AccountFeatureSettings_Status DEFAULT (0),

		[createdBy] [int] NOT NULL,
		[dateCreated] [datetime] NOT NULL,
		[modifiedBy] [int] NULL,
		[dateLastModified] [datetime] NULL,
		[dateDeleted] [datetime] NULL,
		[DeletedBy] [int] NULL,

        -- FKs
        CONSTRAINT FK_AccountFeatureSettings_Features
            FOREIGN KEY (FeatureId) REFERENCES dbo.Features(Id),
					   			       
    );

END
GO
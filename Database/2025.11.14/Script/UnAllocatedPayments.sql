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
GO
 
/****** Object:  Table [dbo].[UnallocatedPayments]    Script Date: 11/5/2025 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 
IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'UnallocatedPayments' 
      AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[UnallocatedPayments](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [AccountInfoId] [int] NOT NULL,
        [PaymentId] [int] NOT NULL,
        [ChildProfileId] [int] NOT NULL,
        [UnallocatedAmount] [decimal](18, 2) NOT NULL CHECK ([UnallocatedAmount] <> 0),
        [Notes] [nvarchar](max) NULL,
        [CreatedBy] [int] NULL,
        [DateCreated] [datetime] NOT NULL CONSTRAINT [DF_UnallocatedPayments_DateCreated] DEFAULT (GETDATE()),
        [ModifiedBy] [int] NULL,
        [DateLastModified] [datetime] NULL,
        [DateDeleted] [datetime] NULL,
        [DeletedBy] [int] NULL,
        CONSTRAINT [PK_UnallocatedPayments] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        ) WITH (
            STATISTICS_NORECOMPUTE = OFF,
            IGNORE_DUP_KEY = OFF,
            OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
        ) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
 
    PRINT '[dbo].[UnallocatedPayments] table created successfully.';
END
ELSE
BEGIN
    PRINT '[dbo].[UnallocatedPayments] already exists. Skipping creation.';
END
GO
 
-- Add Foreign Key Constraint
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_UnallocatedPayments_Payment'
      AND parent_object_id = OBJECT_ID('dbo.UnallocatedPayments')
)
BEGIN
    ALTER TABLE [dbo].[UnallocatedPayments] WITH CHECK 
    ADD CONSTRAINT [FK_UnallocatedPayments_Payment] 
    FOREIGN KEY([PaymentId])
    REFERENCES [dbo].[Payment] ([Id])
    ON DELETE CASCADE
    ON UPDATE CASCADE;
 
    ALTER TABLE [dbo].[UnallocatedPayments] CHECK CONSTRAINT [FK_UnallocatedPayments_Payment];
    PRINT 'Foreign key [FK_UnallocatedPayments_Payment] created successfully.';
END
ELSE
BEGIN
    PRINT 'Foreign key [FK_UnallocatedPayments_Payment] already exists. Skipping.';
END
GO
 
-- Create Index
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Payment_AccountInfo_Member' 
      AND object_id = OBJECT_ID('dbo.UnallocatedPayments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Payment_AccountInfo_Member] 
    ON [dbo].[UnallocatedPayments] (AccountInfoId, PaymentId, ChildProfileId) 
    INCLUDE (UnallocatedAmount, Notes);
    PRINT 'Index [IX_Payment_AccountInfo_Member] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [IX_Payment_AccountInfo_Member] already exists. Skipping.';
END
GO
 
COMMIT TRANSACTION
GO






 
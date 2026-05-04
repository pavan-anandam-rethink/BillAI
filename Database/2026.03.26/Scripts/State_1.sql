IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'State' 
      AND schema_id = SCHEMA_ID('dbo')
)
BEGIN

    CREATE TABLE [dbo].[State](
        [StateId] [int] IDENTITY(1,1) NOT NULL,
        [StateName] [varchar](31) NOT NULL,
        [StateCode] [char](2) NOT NULL,
        [DateCreated] [datetime] NOT NULL,
        [DateLastModified] [datetime] NULL,
        [DateDeleted] [datetime] NULL,
        [UtcOffSet] [int] NULL,
        [UtcDSTOffSet] [int] NULL,
        [ModifiedBy] [int] NULL,
        [DeletedBy] [int] NULL,
        [CreatedBy] [int] NOT NULL,
        [SupportsSandata] [bit] NOT NULL,
        CONSTRAINT [PK_State] PRIMARY KEY CLUSTERED ([StateId] ASC)
    ) ON [PRIMARY];

    ALTER TABLE [dbo].[State] 
        ADD CONSTRAINT [DF__State__DateCre__00200768] DEFAULT (getdate()) FOR [DateCreated];

    ALTER TABLE [dbo].[State] 
        ADD CONSTRAINT [DF__State__DateLas__01142BA1] DEFAULT (getdate()) FOR [DateLastModified];

    ALTER TABLE [dbo].[State] 
        ADD CONSTRAINT [DF__State__SupportsSandata] DEFAULT ((0)) FOR [SupportsSandata];

END

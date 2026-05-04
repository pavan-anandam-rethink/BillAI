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

/****** Object:  Table [dbo].[PaymentsAdjustments]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentsAdjustments]') AND type in (N'U'))
DROP TABLE [dbo].[PaymentsAdjustments]
GO
/****** Object:  Table [dbo].[FunderNameReporting]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FunderNameReporting]') AND type in (N'U'))
DROP TABLE [dbo].[FunderNameReporting]
GO
/****** Object:  Table [dbo].[ClientNameReporting]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClientNameReporting]') AND type in (N'U'))
DROP TABLE [dbo].[ClientNameReporting]
GO
/****** Object:  Table [dbo].[ClaimStatusReporting]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClaimStatusReporting]') AND type in (N'U'))
DROP TABLE [dbo].[ClaimStatusReporting]
GO
/****** Object:  Table [dbo].[AccountsReceivable]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AccountsReceivable]') AND type in (N'U'))
DROP TABLE [dbo].[AccountsReceivable]
GO

IF @@ERROR <> 0 SET NOEXEC ON
GO

COMMIT TRANSACTION
GO

DECLARE @Success AS BIT
SET @Success=1
SET NOEXEC OFF
IF (@Success = 1) PRINT 'Database update succeeded'
ELSE BEGIN
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
	PRINT 'The database update failed. ROLLBACK TRANSACTION'
END
GO
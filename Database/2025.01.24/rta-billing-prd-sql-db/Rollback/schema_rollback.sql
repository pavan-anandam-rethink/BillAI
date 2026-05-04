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

/****** Object:  Table [Reporting].[PaymentsAdjustments]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Reporting].[PaymentsAdjustments]') AND type in (N'U'))
DROP TABLE [Reporting].[PaymentsAdjustments];
GO
/****** Object:  Table [Reporting].[AccountsReceivable]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Reporting].[AccountsReceivable]') AND type in (N'U'))
DROP TABLE [Reporting].[AccountsReceivable]
GO
/****** Object:  Table [Reporting].[Clients]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Reporting].[Clients]') AND type in (N'U'))
DROP TABLE [Reporting].[Clients];
GO
/****** Object:  Table [Reporting].[Funders]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Reporting].[Funders]') AND type in (N'U'))
DROP TABLE [Reporting].[Funders]
GO
/****** Object:  Table [Reporting].[ClaimStatus]    Script Date: 12/3/2024 11:25:11 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Reporting].[ClaimStatus]') AND type in (N'U'))
DROP TABLE [Reporting].[ClaimStatus]
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
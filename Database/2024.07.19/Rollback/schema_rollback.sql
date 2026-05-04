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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimChargeEntryWriteOff') AND type in (N'U'))
BEGIN
	DROP TABLE [dbo].[ClaimChargeEntryWriteOff]
END
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimWriteOff') AND type in (N'U'))
BEGIN
	DROP TABLE [dbo].[ClaimWriteOff]
END
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.WriteOffReasonCode') AND type in (N'U'))
BEGIN
	DROP TABLE [dbo].[WriteOffReasonCode]
END
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.WriteOffAction') AND type in (N'U'))
BEGIN
	DROP TABLE [dbo].[WriteOffAction]  
END
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.WriteOffApplication') AND type in (N'U'))
BEGIN
	DROP TABLE [dbo].[WriteOffApplication]
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Claims') AND type in (N'U'))
BEGIN
	IF EXISTS (SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[Claims]') AND name = 'providerBillingCodeId')
	BEGIN
		--ALTER TABLE dbo.[Claims] ADD providerBillingCodeId INT NULL
		ALTER TABLE dbo.[Claims] DROP COLUMN providerBillingCodeId
	END
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimResponseFileType') AND type in (N'U'))
BEGIN

	--CREATE TABLE [dbo].[ClaimResponseFileType]([Id] [int] NOT NULL,[FileType] [varchar](50) NULL,
	--PRIMARY KEY CLUSTERED ([Id] ASC)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]
	DROP TABLE [dbo].[ClaimResponseFileType]
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClearingHouseResponseDetails') AND type in (N'U'))
BEGIN
	DROP TABLE dbo.ClearingHouseResponseDetails
END
GO

-- CHETAN MALI - 210825
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimValidationErrors') AND type in (N'U'))
BEGIN
	IF EXISTS (SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[ClaimValidationErrors]') AND name = 'refValidationId')
	BEGIN
		--ALTER TABLE dbo.[ClaimValidationErrors] ADD refValidationId INT NULL
		ALTER TABLE dbo.[ClaimValidationErrors] DROP COLUMN refValidationId
	END
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimChargeEntries') AND type in (N'U'))
BEGIN
	IF EXISTS (SELECT * FROM sys.columns WHERE  object_id = OBJECT_ID(N'[dbo].[ClaimChargeEntries]') AND name = 'billingCodeId')
	BEGIN
		--ALTER TABLE dbo.[ClaimChargeEntries] ADD billingCodeId INT NULL
		ALTER TABLE dbo.[ClaimChargeEntries] DROP COLUMN billingCodeId
	END
END
GO

/****** Object:  StoredProcedure [dbo].[GetClaimsByAccountInfoId]    Script Date: 6/28/2024 11:33:54 AM ******/
--PROCEDURE SCRIPT FROM STAGE
ALTER PROCEDURE [dbo].[GetClaimsByAccountInfoId]
(
	@AccountInfoId int,     
	@Skip int, @Take int, 
	@OrderField nvarchar(40) = '',
	@OrderDir bit = null,

	@ClaimNumber nvarchar(20) = null,
	@ClaimIds nvarchar(200) = null,
	@PatientIds nvarchar(100) = null,
	@FunderIds nvarchar(100) = null,
	@BalanceFrom decimal(18,2) = null,
	@BalanceTo decimal(18,2) = null,
	@BilledFrom decimal(18,2) = null,
    @BilledTo decimal(18,2) = null,
	@PatientResponsibilityFrom int = null,
    @PatientResponsibilityTo int = null,
	@RenderingProviderIds nvarchar(100) = null,
	@StatusIds nvarchar(100) = null,
	@Tab int,
	@ShowVoided bit,
	@ValidationIds nvarchar(100) = null
)
AS
BEGIN
    SET NOCOUNT ON

	DECLARE @ShowWithNoErrorsIndicator INT = 99;

	SELECT DISTINCT   
			[c].[Id] as Id,
			[c].[ClaimIdentifier] as ClaimNumber,
			COALESCE([c].[authorizationNumber], 'N/A') as AuthorizationNumber,
			[c].[childProfileId] as ChildProfileId,
			[c].[hcChildProfileAuthorizationId] as ChildProfileAuthorizationId,
			[c].[hcLocationCodeId] as LocationCodeId,
			[c].[startDate] as DateOfServiceStart,
			[c].[endDate] as DateOfServiceEnd,
			[renderingProviderSearchClaim].[id] as RenderingProviderId,
			[cce3].[Charges] as BilledAmount,
			ISNULL([cce3].[Charges], 0) as ExpectedAmount,
			ISNULL([calcNew].[patientRespAmount], 0) as  PatientResponsibilityAmount,
			ISNULL([calcNew].[totalPayment], 0) as PaymentAmount,
			ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL([calc].Adjustment,0) - ISNULL([calcNew].[patientRespAmount], 0) as BalanceAmount,
			ISNULL([calc].Adjustment,0) as AdjustmentAmount,
			[c].[hcClaimStatusId] as Status,
			[cs].[name] as ClaimStatusName,
			[csub].[hcClaimSubmissionId],
			[csub].[claimSubmissionIdentifier],
			[csub].[documentTypeId],
			[csub].[submissionTypeId],
			[csub].[frequencyTypeId],
			[csub].[submissionStatusId],
			[csub].[submissionStatusName],
			[csub].[SubmitDate],
			[funderSearchClaim].[id] as FunderId,
			COALESCE([clientSearchClaim].[name], '') as PatientName,
			[cvec].[ErrorsCount],
			[cvec].[WarningsCount],
			CAST(CEILING(CAST([cce3].[NoOfCharges] as float)/6) as int) as [CMSPagesCount], -- For CMS1500 (Max. 6 charges allowed per CMS1500)
			COALESCE([locationCodes].[Name], '') as PlaceOfService,
			[c].[clientFunderId] as ChildProfileFunderId,
			[funderSearchClaim].[name] as FunderName,
			[renderingProviderSearchClaim].[name] AS RenderingProviderName,
			CAST(CASE 
					WHEN [cn].[hcClaimId] IS NOT NULL THEN 1 
					ELSE 0 end as BIT) AS HasNote
			,c.billedDate AS BilledDate
	into #temp
	
	FROM [dbo].[Claims] as [c]
	LEFT JOIN (SELECT [hcClaimId] FROM [ClaimNotes]) cn ON [c].[id] = [cn].[hcClaimId]
	LEFT JOIN [ClaimChargeEntries] AS [cce] ON [c].[id] = [cce].[hcClaimId]
	LEFT JOIN [ClaimAppointmentLink] as [cal] ON [c].[id] = [cal].[hcClaimId]
	LEFT JOIN [ClaimStatus] as [cs] ON [c].[hcClaimStatusId] = [cs].[id]
	LEFT JOIN (SELECT [cce2].[hcClaimId],
				SUM([cce2].[Charges]) AS Charges,
				COUNT([cce2].[Id]) AS NoOfCharges,
				MIN([cce2].[DateOfService]) as DateOfServiceStart,
				MAX([cce2].[DateOfService]) as DateOfServiceEnd
				FROM [ClaimChargeEntries] AS cce2
				where [cce2].[DateDeleted] is null
				GROUP BY [cce2].[hcClaimId]
				) cce3 ON [c].[Id] = [cce3].[hcClaimId]

	LEFT JOIN(
		SELECT DISTINCT pc.hcClaimId,
		SUM(pcladj.adjustmentAmount) as Adjustment	
		FROM [dbo].[Paymentclaim] pc 
		INNER JOIN 
		[dbo].[PaymentClaimServiceLine] pcsl ON pc.id = pcsl.hcPaymentClaimId
		LEFT JOIN 
		[dbo].[PaymentClaimServiceLineAdjustment] pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
		where pc.dateDeleted is null and pcladj.dateDeleted is null and pcladj.adjustmentGroupCode != 'PR'
		GROUP by pc.hcClaimId
	) AS  calc on calc.hcClaimId=c.Id

	LEFT JOIN(
		SELECT DISTINCT 
		pc.hcClaimId,
		MAX(COALESCE(pc.totalPayment, 0)) AS totalPayment
		,SUM(COALESCE(pc.patientRespAmount, 0)) AS patientRespAmount	
		from [dbo].[Paymentclaim] pc
		GROUP by pc.hcClaimId
	) AS calcNew on calcNew.hcClaimId=c.Id 

    LEFT JOIN (
        SELECT 
            cs.Id AS hcClaimSubmissionId,
            cs.hcClaimId,
            cs.hcFunderId,
            cs.claimSubmissionIdentifier,
            cs.documentTypeId,
            cs.submissionTypeId,
            cs.frequencyTypeId,
            cs.submissionStatusId,
            css.Name AS submissionStatusName,
            cs.SubmitDate
        FROM ClaimSubmissions cs
        LEFT JOIN ClaimSubmissionStatus css on css.Id = cs.submissionStatusId
        JOIN (
            SELECT hcClaimId, MAX(Id) LatestSubmissionId
            FROM dbo.ClaimSubmissions 
            WHERE dateDeleted is null
            GROUP BY hcClaimId
            ) AS csl on csl.LatestSubmissionId = cs.Id
        ) csub on csub.hcClaimId = c.Id
		LEFT JOIN ClaimSearchClients clientSearchClaim on clientSearchClaim.id = c.childProfileId
		LEFT JOIN ClaimSearchChildProfileAuthorizations childProfileAuths on childProfileAuths.id = c.hcChildProfileAuthorizationId and childProfileAuths.dateDeleted is null
		LEFT JOIN ClaimSearchFunders funderSearchClaim on funderSearchClaim.id = (select top 1 id from [ClaimSearchFunders] where id in ([csub].[hcFunderId], [c].[lastBilledFunderId], [childProfileAuths].[funderId]))
		LEFT JOIN ClaimSearchRenderingProviders renderingProviderSearchClaim on renderingProviderSearchClaim.id = 
		-- ADDED BY CHETAN TO RESOLVE BLANK RENDERING PROVIDER ISSUE ON DASHBOARD
		(case when c.renderingStaffMemberId = -2 then c.tblMemberId when c.renderingStaffMemberId is null then c.tblMemberId else c.renderingStaffMemberId end) and renderingProviderSearchClaim.dateDeleted is null
		LEFT JOIN ClaimSearchLocations locationCodes on locationCodes.Id = c.hcLocationCodeId and locationCodes.dateDeleted is null
		LEFT JOIN [dbo].[ClaimValidationErrors] cve on cve.claimId = c.id
		LEFT JOIN [dbo].[ClaimErrorMessages] cem on cem.id = cve.claimErrorMessageId
		LEFT JOIN (
			SELECT hcClaimId, [2] as ErrorsCount, [3] as WarningsCount FROM (SELECT cv.claimId as ClaimId, cer.severity, cv.claimId as hcClaimId
				FROM [dbo].[ClaimValidationErrors] cv
				LEFT JOIN [dbo].[ClaimErrorMessages] as cer on cer.id = cv.claimErrorMessageId
				WHERE cv.dateDeleted is null AND cv.claimId is not null
			) st
			PIVOT
			(
				count(ClaimId)
				FOR severity in ([2], [3])
			) pvt
		) cvec on cvec.hcClaimId = c.id
	WHERE ([c].[AccountInfoId] = @AccountInfoId) AND [c].[DateDeleted] IS NULL AND
	((@Tab = 1 AND [c].[hcClaimStatusId] = 1 AND [c].[isFlagged] = 0 OR 
	(@Tab = 2 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 7 OR [c].[hcClaimStatusId] = 10 OR [c].[hcClaimStatusId] = 11 OR [c].[hcClaimStatusId] = 14) AND [c].[isFlagged] = 0) OR
	(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 12) AND [c].[isFlagged] = 0) OR
	(@Tab = 4 AND ((@ShowVoided = 1 AND [c].[hcClaimStatusId] = 2) OR [c].[hcClaimStatusId] = 6 OR [c].[hcClaimStatusId] = 13) AND [c].[isFlagged] = 0) OR
	(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
	(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
	(@Tab = 7 AND ([c].[isFlagged] = 1))))
		AND [c].[ClaimIdentifier] LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END

	AND (@PatientIds is null OR @PatientIds = '' OR [clientSearchClaim].[id] in (select * from STRING_SPLIT(@PatientIds, ',')))

	AND (@ClaimIds is null OR @ClaimIds = '' OR [c].[id] in (select * from STRING_SPLIT(@ClaimIds, ',')))

	AND (@FunderIds is null OR @FunderIds = '' OR [funderSearchClaim].[id] in (select * from STRING_SPLIT(@FunderIds, ',')))

	AND (@RenderingProviderIds is null OR @RenderingProviderIds = '' OR [renderingProviderSearchClaim].[id] in (select * from STRING_SPLIT(@RenderingProviderIds, ',')))

	AND (@BalanceFrom IS NULL OR @BalanceFrom <= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL([calc].Adjustment,0) - ISNULL([calcNew].[patientRespAmount], 0))												 

	AND (@BalanceTo IS NULL OR @BalanceTo >= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0)+ ISNULL([calc].Adjustment,0) - ISNULL([calcNew].[patientRespAmount], 0))

	AND (@PatientResponsibilityFrom IS NULL OR @PatientResponsibilityFrom <= ISNULL([calcNew].[patientRespAmount], 0))
    AND (@PatientResponsibilityTo IS NULL OR @PatientResponsibilityTo >= ISNULL([calcNew].[patientRespAmount], 0))

	AND (@BilledFrom IS NULL OR @BilledFrom <= [cce3].[Charges])
    AND (@BilledTo IS NULL OR @BilledTo >= [cce3].[Charges])

	AND (@StatusIds IS NULL OR @StatusIds = '' OR [c].[hcClaimStatusId] IN (SELECT * FROM STRING_SPLIT(@StatusIds, ',')))

	AND (@ValidationIds IS NULL OR @ValidationIds = '' OR cem.severity IN
		(SELECT * FROM STRING_SPLIT(@ValidationIds, ',')) OR
			(cvec.WarningsCount IS NULL AND cvec.ErrorsCount IS NULL AND @ShowWithNoErrorsIndicator IN
				(SELECT * FROM STRING_SPLIT(@ValidationIds, ','))
		    )
		)

	select *, COUNT(*) OVER () as totalCount from #temp as c
		order by  
		case when @OrderField = 'claimNumber' and @OrderDir = 0 then c.ClaimNumber end asc,
		case when @OrderField = 'claimNumber' and @OrderDir = 1 then c.ClaimNumber end desc,
				
		case when @OrderField = 'DateOfServiceStart' and @OrderDir = 0 then c.DateOfServiceStart end asc,
		case when @OrderField = 'DateOfServiceStart' and @OrderDir = 1 then c.DateOfServiceStart end desc,
		
		case when @OrderField = 'DateOfServiceEnd' and @OrderDir = 0 then c.DateOfServiceEnd end asc,
		case when @OrderField = 'DateOfServiceEnd' and @OrderDir = 1 then c.DateOfServiceEnd end desc,
		
		case when @OrderField = 'BilledAmount' and @OrderDir = 0 then c.BilledAmount end asc,
		case when @OrderField = 'BilledAmount' and @OrderDir = 1 then c.BilledAmount end desc,
		
		case when @OrderField = 'PatientResponsibilityAmount' and @OrderDir = 0 then c.PatientResponsibilityAmount end asc,
		case when @OrderField = 'PatientResponsibilityAmount' and @OrderDir = 1 then c.PatientResponsibilityAmount end desc,
		
		case when @OrderField = 'ExpectedAmount' and @OrderDir = 0 then c.ExpectedAmount end asc,
		case when @OrderField = 'ExpectedAmount' and @OrderDir = 1 then c.ExpectedAmount end desc,
		
		case when @OrderField = 'PaymentAmount' and @OrderDir = 0 then c.PaymentAmount end asc,
		case when @OrderField = 'PaymentAmount' and @OrderDir = 1 then c.PaymentAmount end desc,
				
		case when @OrderField = 'BalanceAmount' and @OrderDir = 0 then c.BalanceAmount end asc,
		case when @OrderField = 'BalanceAmount' and @OrderDir = 1 then c.BalanceAmount end desc,
				
		case when @OrderField = 'Status' and @OrderDir = 0 then c.Status end asc,
		case when @OrderField = 'Status' and @OrderDir = 1 then c.Status end desc,

		case when @OrderField = 'PatientName' and @OrderDir = 0 then c.patientName end asc,
		case when @OrderField = 'PatientName' and @OrderDir = 1 then c.patientName end desc,
		
		case when @OrderField = 'FunderName' and @OrderDir = 0 then c.funderName end asc,
		case when @OrderField = 'FunderName' and @OrderDir = 1 then c.funderName end desc,

		case when @OrderField = 'AuthorizationNumber' and @OrderDir = 0 then c.AuthorizationNumber end asc,
		case when @OrderField = 'AuthorizationNumber' and @OrderDir = 1 then c.AuthorizationNumber end desc,

		case when @OrderField = 'PlaceOfService' and @OrderDir = 0 then c.PlaceOfService end asc,
		case when @OrderField = 'PlaceOfService' and @OrderDir = 1 then c.PlaceOfService end desc,

		case when @OrderField = 'RenderingProviderName' and @OrderDir = 0 then c.RenderingProviderName end asc,
		case when @OrderField = 'RenderingProviderName' and @OrderDir = 1 then c.RenderingProviderName end desc

	offset @Skip rows
	fetch next @Take rows only

	drop table #temp
END


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
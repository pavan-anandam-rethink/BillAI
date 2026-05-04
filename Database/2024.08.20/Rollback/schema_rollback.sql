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

EXEC SP_RENAME 'ClaimSearchClients.lastName', 'name', 'COLUMN'
GO
ALTER TABLE [dbo].[ClaimSearchClients]
  DROP COLUMN firstName
GO
ALTER TABLE [dbo].[ClaimSearchClients]
  DROP COLUMN middleName
GO

/****** Object:  StoredProcedure [dbo].[GetClaimsCount]    Script Date: 7/22/2024 12:18:11 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetClaimsCount]
(
	@AccountInfoId INT,
	@ClaimNumber NVARCHAR(20) = null,
	@ShowVoided BIT
)
AS
BEGIN
    SET NOCOUNT ON
		
    SELECT DISTINCT   
            [c].[Id] AS Id,
			[c].claimIdentifier AS ClaimNumber
	INTO #temp
	
    FROM [Claims] AS [c]
    LEFT JOIN [ClaimChargeEntries] AS [cce] ON [c].[id] = [cce].[hcClaimId]
	WHERE ([c].[AccountInfoId] = @AccountInfoId) AND [c].[DateDeleted] IS NULL
	AND [c].[ClaimIdentifier] LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END

	SELECT 
		COUNT (CASE WHEN claim.[hcClaimStatusId] = 1 AND claim.[isFlagged] = 0 THEN 1 END) AS PendingReviewTotalCount,
		COUNT (CASE WHEN (claim.[hcClaimStatusId] = 2 OR claim.[hcClaimStatusId] = 7 OR claim.[hcClaimStatusId] = 10 OR claim.[hcClaimStatusId] = 11) AND claim.[isFlagged] = 0 THEN 1 END) AS ReadyToBillTotalCount,
		COUNT (CASE WHEN (claim.[hcClaimStatusId] = 3 OR claim.[hcClaimStatusId] = 4 OR claim.[hcClaimStatusId] = 12) AND claim.[isFlagged] = 0 THEN 1 END) AS BillingPendingTotalCount,
		COUNT (CASE WHEN ((@ShowVoided = 1 AND claim.[hcClaimStatusId] = 2) OR claim.[hcClaimStatusId] = 6) AND claim.[isFlagged] = 0 THEN 1 END) AS ClosedTotalCount,
		COUNT (CASE WHEN (claim.[hcClaimStatusId] = 8 OR claim.[hcClaimStatusId] = 9) AND claim.[isFlagged] = 0 THEN 1 END) AS RejectedTotalCount,
		COUNT (CASE WHEN (claim.[hcClaimStatusId] = 5) AND claim.[isFlagged] = 0 THEN 1 END) AS DeniedTotalCount,
		COUNT (CASE WHEN claim.[isFlagged] = 1 THEN 1 END) AS FlaggedTotalCount	
	FROM #temp t
	LEFT JOIN Claims AS claim ON claim.id = t.Id

	DROP TABLE #temp
END
GO


GO

/****** Object:  StoredProcedure [dbo].[GetClaimsPatientsFilters]    Script Date: 7/22/2024 12:18:41 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[GetClaimsPatientsFilters] --EXEC [dbo].[GetClaimsPatientsFilters] 18421, 1
(
    @AccountInfoId int,
	@Tab int
)
AS
BEGIN
    SET NOCOUNT ON

    SELECT [clientSearchClaim].[id], [clientSearchClaim].[name]
	FROM [dbo].[Claims] as [c]
		LEFT JOIN ClaimSearchClients clientSearchClaim on clientSearchClaim.id = c.childProfileId
	WHERE 
		[c].[AccountInfoId] = @AccountInfoId
		AND [c].[DateDeleted] IS NULL
		AND --[c].[renderingStaffMemberId] IS NOT NULL AND
		((@Tab = 1 AND ([c].[hcClaimStatusId] = 1 AND [c].[isFlagged] = 0)) OR 
		(@Tab = 2 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 7 OR [c].[hcClaimStatusId] = 10 OR [c].[hcClaimStatusId] = 11) AND [c].[isFlagged] = 0) OR
		(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 12) AND [c].[isFlagged] = 0) OR
		(@Tab = 4 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 6) AND [c].[isFlagged] = 0) OR
		(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
		(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
		(@Tab = 7 AND ([c].[isFlagged] = 1)))
	GROUP BY [clientSearchClaim].[id], [clientSearchClaim].[name]
	ORDER BY [clientSearchClaim].[name]
END
GO



/****** Object:  StoredProcedure [dbo].[GetClaimsFundersFilters]    Script Date: 7/22/2024 12:19:42 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[GetClaimsFundersFilters] --EXEC [dbo].[GetClaimsFundersFilters] 18421, 1
(
	@AccountInfoId int,
	@Tab int
)
AS
BEGIN
    SET NOCOUNT ON

	SELECT funderSearch.id, funderSearch.name FROM [dbo].[Claims] as [c]
	LEFT JOIN (
        SELECT 
            cs.Id AS hcClaimSubmissionId,
            cs.hcClaimId,
            cs.hcFunderId,
            cs.SubmitDate
        FROM ClaimSubmissions cs
        JOIN (
            SELECT hcClaimId, MAX(Id) LatestSubmissionId
            FROM ClaimSubmissions 
            WHERE dateDeleted is null
            GROUP BY hcClaimId
            ) AS csl on csl.LatestSubmissionId = cs.Id
        ) csub on csub.hcClaimId = c.Id
	LEFT JOIN ClaimSearchChildProfileAuthorizations childProfileAuths on childProfileAuths.id = c.hcChildProfileAuthorizationId and childProfileAuths.dateDeleted is null
	LEFT JOIN ClaimSearchFunders funderSearch on funderSearch.id = (select top 1 id from [ClaimSearchFunders] where id in ([csub].[hcFunderId], [c].[lastBilledFunderId], [childProfileAuths].[funderId]) and dateDeleted is null)
	WHERE 
		[c].[AccountInfoId] = @AccountInfoId
		AND [c].[DateDeleted] IS NULL
		AND [c].[claimIdentifier] IS NOT NULL
		AND --[c].[renderingStaffMemberId] IS NOT NULL AND
		((@Tab = 1 AND ([c].[hcClaimStatusId] = 1 AND [c].[isFlagged] = 0)) OR 
		(@Tab = 2 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 7 OR [c].[hcClaimStatusId] = 10 OR [c].[hcClaimStatusId] = 11) AND [c].[isFlagged] = 0) OR
		(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 12) AND [c].[isFlagged] = 0) OR
		(@Tab = 4 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 6) AND [c].[isFlagged] = 0) OR
		(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
		(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
		(@Tab = 7 AND ([c].[isFlagged] = 1)))
	GROUP BY funderSearch.id, funderSearch.name
	ORDER BY funderSearch.name 
END
GO
/****** Object:  StoredProcedure [dbo].[GetClaimsRPFilters]    Script Date: 7/22/2024 12:20:14 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[GetClaimsRPFilters]
(
	@AccountInfoId int,
	@Tab int
)
AS
BEGIN
    SET NOCOUNT ON

    SELECT [rpSearch].[id], [rpSearch].[name]
	FROM [dbo].[Claims] as [c]
		LEFT JOIN ClaimSearchRenderingProviders rpSearch on rpSearch.id = (case c.renderingStaffMemberId when -2 then c.tblMemberId else c.renderingStaffMemberId end) and rpSearch.dateDeleted is null
	WHERE 
		[c].[AccountInfoId] = @AccountInfoId
		AND [c].[DateDeleted] IS NULL
		AND [c].[claimIdentifier] IS NOT NULL
		AND --[c].[renderingStaffMemberId] IS NOT NULL AND
		((@Tab = 1 AND ([c].[hcClaimStatusId] = 1 AND [c].[isFlagged] = 0)) OR 
		(@Tab = 2 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 7 OR [c].[hcClaimStatusId] = 10 OR [c].[hcClaimStatusId] = 11) AND [c].[isFlagged] = 0) OR
		(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 12) AND [c].[isFlagged] = 0) OR
		(@Tab = 4 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 6) AND [c].[isFlagged] = 0) OR
		(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
		(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
		(@Tab = 7 AND ([c].[isFlagged] = 1)))
	GROUP BY [rpSearch].[id], [rpSearch].[name]
	ORDER BY [rpSearch].[name]
END
GO


/****** Object:  StoredProcedure [dbo].[GetClaimsByAccountInfoId]    Script Date: 7/25/2024 4:59:50 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


-->

--CHANGES BY CHETAN - 16072024
--TO SHOW THE FUNDER/CLEARINGHOUSE - ACCEPTED STATUS CLAIMS ON DASHBOARD
--START
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

WITH CTE_PositiveAdjustments AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS PositiveAdjustment
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON
pc.id
= pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON
pcsl.id
= pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode != 'PR'
         AND pcladj.IsAdjustmentPositive = 1
   GROUP BY pc.hcClaimId
),
CTE_NegativeAdjustments AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS NegativeAdjustment
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON
pc.id
= pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON
pcsl.id
= pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode != 'PR'
         AND pcladj.IsAdjustmentPositive = 0
   GROUP BY pc.hcClaimId
),
CTE_Calculations AS (
   SELECT DISTINCT pc.hcClaimId,
          MAX(COALESCE(pc.totalPayment, 0)) AS TotalPayment,
          SUM(COALESCE(pc.patientRespAmount, 0)) AS PatientRespAmount
   FROM [dbo].[Paymentclaim] pc
   GROUP BY pc.hcClaimId
),
CTE_WriteOff AS (
   SELECT DISTINCT cw.ClaimId,
   SUM(ccew.writeOffAmount) AS WriteOffAmount
   FROM [dbo].[ClaimWriteOff] cw
   INNER JOIN [dbo].[ClaimChargeEntryWriteOff] ccew ON
cw.Id = ccew.ClaimWriteOffId
   WHERE cw.dateDeleted IS NULL
         AND ccew.dateDeleted IS NULL
   GROUP BY cw.ClaimId
),
CTE_Charges AS (
   SELECT cce2.hcClaimId,
          SUM(cce2.Charges) AS Charges,
          COUNT(cce2.Id) AS NoOfCharges,
          MIN(cce2.DateOfService) AS DateOfServiceStart,
          MAX(cce2.DateOfService) AS DateOfServiceEnd
   FROM [ClaimChargeEntries] AS cce2
   WHERE cce2.DateDeleted IS NULL
   GROUP BY cce2.hcClaimId
),
CTE_ResponseCount AS (
	select COUNT(*) as RespCount,cl.ClaimId from dbo.ClearingHouseResponseDetails cl left join dbo.ClaimValidationErrors cv
on cl.claimValidationErrorId = cv.id
where (refvalidationid is null or refvalidationid = 0)
group by cl.ClaimId

)

SELECT DISTINCT
      c.Id AS Id,
      c.ClaimIdentifier AS ClaimNumber,
      COALESCE(c.authorizationNumber, 'N/A') AS AuthorizationNumber,
      c.childProfileId AS ChildProfileId,
      c.hcChildProfileAuthorizationId AS ChildProfileAuthorizationId,
      c.hcLocationCodeId AS LocationCodeId,
      c.startDate AS DateOfServiceStart,
      c.endDate AS DateOfServiceEnd,
renderingProviderSearchClaim.id
AS RenderingProviderId,
      cce3.Charges AS BilledAmount,
      ISNULL(cce3.Charges, 0) AS ExpectedAmount,
      ISNULL(calcNew.PatientRespAmount, 0) AS PatientResponsibilityAmount,
      ISNULL(calcNew.TotalPayment, 0) AS PaymentAmount,
      ISNULL(cce3.Charges, 0) - ISNULL(calcNew.TotalPayment, 0)
          + ISNULL(calc.PositiveAdjustment, 0)
          - ISNULL(calc1.NegativeAdjustment, 0)
		  - ISNULL(calc2.WriteOffAmount,0)
          - ISNULL(calcNew.PatientRespAmount, 0) AS BalanceAmount,
      ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) AS AdjustmentAmount,
      c.hcClaimStatusId AS Status,
      cs.name AS ClaimStatusName,
      csub.hcClaimSubmissionId,
      csub.claimSubmissionIdentifier,
      csub.documentTypeId,
      csub.submissionTypeId,
      csub.frequencyTypeId,
      csub.submissionStatusId,
      csub.submissionStatusName,
      csub.SubmitDate,
funderSearchClaim.id
AS FunderId,
      COALESCE(clientSearchClaim.name, '') AS PatientName,
      cvec.ErrorsCount,
      cvec.WarningsCount,
	  ccrc.RespCount as ResponseCount,
      CAST(CEILING(CAST(cce3.NoOfCharges AS float) / 6) AS int) AS CMSPagesCount, -- For CMS1500 (Max. 6 charges allowed per CMS1500)
      COALESCE(locationCodes.Name, '') AS PlaceOfService,
      c.clientFunderId AS ChildProfileFunderId,
      funderSearchClaim.name AS FunderName,
      renderingProviderSearchClaim.name AS RenderingProviderName,
      CAST(CASE
             WHEN cn.hcClaimId IS NOT NULL THEN 1
             ELSE 0
          END AS BIT) AS HasNote,
      c.billedDate AS BilledDate
INTO #temp
FROM [dbo].[Claims] AS c
LEFT JOIN CTE_PositiveAdjustments AS calc ON calc.hcClaimId = c.Id
LEFT JOIN CTE_NegativeAdjustments AS calc1 ON calc1.hcClaimId = c.Id
LEFT JOIN CTE_WriteOff AS calc2 ON calc2.ClaimId = c.Id
LEFT JOIN CTE_Calculations AS calcNew ON calcNew.hcClaimId = c.Id
LEFT JOIN CTE_Charges AS cce3 ON c.Id = cce3.hcClaimId
LEFT JOIN CTE_ResponseCount AS ccrc ON ccrc.ClaimId = c.id
LEFT JOIN [ClaimChargeEntries] AS cce ON c.Id = cce.hcClaimId
LEFT JOIN [ClaimAppointmentLink] AS cal ON c.Id = cal.hcClaimId
LEFT JOIN [ClaimStatus] AS cs ON c.hcClaimStatusId = cs.Id
LEFT JOIN (
   SELECT cs.Id AS hcClaimSubmissionId,
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
   LEFT JOIN ClaimSubmissionStatus css ON css.Id = cs.submissionStatusId
   JOIN (
       SELECT hcClaimId, MAX(Id) AS LatestSubmissionId
       FROM dbo.ClaimSubmissions
       WHERE dateDeleted IS NULL
       GROUP BY hcClaimId
   ) AS csl ON csl.LatestSubmissionId = cs.Id
) AS csub ON csub.hcClaimId = c.Id
LEFT JOIN ClaimSearchClients AS clientSearchClaim ON clientSearchClaim.Id = c.childProfileId
LEFT JOIN ClaimSearchChildProfileAuthorizations AS childProfileAuths ON childProfileAuths.Id = c.hcChildProfileAuthorizationId AND childProfileAuths.dateDeleted IS NULL
LEFT JOIN ClaimSearchFunders AS funderSearchClaim ON funderSearchClaim.Id = (SELECT TOP 1 Id FROM ClaimSearchFunders WHERE Id IN (csub.hcFunderId, c.lastBilledFunderId, childProfileAuths.funderId))
LEFT JOIN ClaimSearchRenderingProviders AS renderingProviderSearchClaim ON renderingProviderSearchClaim.Id = CASE
                                                                                                             WHEN c.renderingStaffMemberId = -2 THEN c.tblMemberId
                                                                                                             WHEN c.renderingStaffMemberId IS NULL THEN c.tblMemberId
                                                                                                             ELSE c.renderingStaffMemberId
                                                                                                         END AND renderingProviderSearchClaim.dateDeleted IS NULL
LEFT JOIN ClaimSearchLocations AS locationCodes ON locationCodes.Id = c.hcLocationCodeId AND locationCodes.dateDeleted IS NULL
LEFT JOIN [dbo].[ClaimValidationErrors] AS cve ON cve.claimId = c.Id
LEFT JOIN [dbo].[ClaimErrorMessages] AS cem ON cem.Id = cve.claimErrorMessageId
LEFT JOIN (
   SELECT hcClaimId, [2] AS ErrorsCount, [3] AS WarningsCount
   FROM (
       SELECT cv.claimId AS ClaimId, cer.severity, cv.claimId AS hcClaimId
       FROM [dbo].[ClaimValidationErrors] cv
       LEFT JOIN [dbo].[ClaimErrorMessages] AS cer ON cer.Id = cv.claimErrorMessageId
       WHERE cv.dateDeleted IS NULL AND cv.claimId IS NOT NULL
   ) AS st
   PIVOT (
       COUNT(ClaimId)
       FOR severity IN ([2], [3])
   ) AS pvt
) AS cvec ON cvec.hcClaimId = c.Id
LEFT JOIN (SELECT hcClaimId FROM [ClaimNotes]) AS cn ON c.Id = cn.hcClaimId 
	WHERE ([c].[AccountInfoId] = @AccountInfoId) AND [c].[DateDeleted] IS NULL AND 
	((@Tab = 1 AND [c].[hcClaimStatusId] = 1 AND [c].[isFlagged] = 0 OR 
	(@Tab = 2 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 7 OR [c].[hcClaimStatusId] = 10 OR [c].[hcClaimStatusId] = 11 OR [c].[hcClaimStatusId] = 14) AND [c].[isFlagged] = 0) OR
	(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 12  OR [c].[hcClaimStatusId] = 15 OR [c].[hcClaimStatusId] = 16 OR [c].[hcClaimStatusId] = 17) AND [c].[isFlagged] = 0) OR
	(@Tab = 4 AND ((@ShowVoided = 1 AND [c].[hcClaimStatusId] = 2) OR [c].[hcClaimStatusId] = 6 OR [c].[hcClaimStatusId] = 13) AND [c].[isFlagged] = 0) OR
	(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
	(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
	(@Tab = 7 AND ([c].[isFlagged] = 1))))
		AND [c].[ClaimIdentifier] LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END

	AND (@PatientIds is null OR @PatientIds = '' OR [clientSearchClaim].[id] in (select * from STRING_SPLIT(@PatientIds, ',')))

	AND (@ClaimIds is null OR @ClaimIds = '' OR [c].[id] in (select * from STRING_SPLIT(@ClaimIds, ',')))

	AND (@FunderIds is null OR @FunderIds = '' OR [funderSearchClaim].[id] in (select * from STRING_SPLIT(@FunderIds, ',')))

	AND (@RenderingProviderIds is null OR @RenderingProviderIds = '' OR [renderingProviderSearchClaim].[id] in (select * from STRING_SPLIT(@RenderingProviderIds, ',')))

	AND (@BalanceFrom IS NULL OR @BalanceFrom <= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) - ISNULL([calcNew].[patientRespAmount], 0))												 

	AND (@BalanceTo IS NULL OR @BalanceTo >= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) - ISNULL([calcNew].[patientRespAmount], 0))

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
--END

IF @@ERROR <> 0 SET NOEXEC ON
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
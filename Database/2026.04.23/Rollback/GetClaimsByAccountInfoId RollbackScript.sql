/****** Object:  StoredProcedure [dbo].[GetClaimsByAccountInfoId]    Script Date: 17-04-2026 14:37:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[GetClaimsByAccountInfoId]
-- EXEC [dbo].[GetClaimsByAccountInfoId] 18421,0,20,'dateOfServiceStart',1,NULL,NULL,'','','','',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'','',1,0,NULL,NULL,'';  
	
(
	@AccountInfoId int,     
	@Skip int,
	@Take int, 
	@OrderField nvarchar(40) = '',
	@OrderDir bit = null,
	@ClaimNumber nvarchar(20) = null,
	@ClaimIds nvarchar(200) = null,
	@PatientIds nvarchar(100) = null,
	@FunderIds nvarchar(100) = null,
	@AssigneeIds nvarchar(200) = null,
	@LocationIds nvarchar(200) = null,
	@ReasonIds  nvarchar(200) = null,							  
	@BalanceFrom decimal(18,2) = null,
	@BalanceTo decimal(18,2) = null,
	@BilledFrom decimal(18,2) = null,
    @BilledTo decimal(18,2) = null,
	@PatientResponsibilityFrom int = null,
    @PatientResponsibilityTo int = null,
	@DateOfServiceFrom DATETIME = NULL,
	@DateOfServiceTo DATETIME = NULL,
	@RenderingProviderIds nvarchar(100) = null,
	@StatusIds nvarchar(100) = null,
	@Tab int,
	@ShowVoided bit,
	@ValidationIds nvarchar(100) = null,
	@ResponseIds nvarchar(100) = null,
	@ReasonCode nvarchar(100) = null
)
AS
BEGIN
    SET NOCOUNT ON

	DECLARE @ShowWithNoErrorsIndicator INT = 99;

WITH CTE_ReasonCodeForDeniedClaim AS (
   SELECT DISTINCT pc.hcClaimId,
		  STRING_AGG(pcladj.adjustmentReasonCode, ', ') AS ReasonCodes
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON pc.id = pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
   INNER JOIN [dbo].[Claims] c on pc.hcClaimId = c.Id
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND (pc.claimStatus = 4 or c.hcClaimStatusId in (8, 9, 5))  -- we need DeniedReason for only Denied Claims
   GROUP BY pc.hcClaimId
),
CTE_PositiveAdjustments AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS PositiveAdjustment
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON pc.id = pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode != 'PR'
         AND pcladj.IsAdjustmentPositive = 1
		 --AND pc.claimStatus not in ('22') --AND pc.claimStatusOrig is not NULL
   GROUP BY pc.hcClaimId
),
CTE_NegativeAdjustments AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS NegativeAdjustment
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON pc.id = pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode != 'PR'
         AND pcladj.IsAdjustmentPositive = 0
		 --AND pc.claimStatus not in ('22') --AND pc.claimStatusOrig is not NULL
   GROUP BY pc.hcClaimId
),
CTE_PositivePatientResponsibility AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS PositivePatientResponsibility
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON pc.id = pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode = 'PR'
         AND pcladj.IsAdjustmentPositive = 1
		 --AND pc.claimStatus not in ('22') --AND pc.claimStatusOrig is not NULL
   GROUP BY pc.hcClaimId
),
CTE_NegativePatientResponsibility AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS NegativePatientResponsibility
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON pc.id = pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode = 'PR'
         AND pcladj.IsAdjustmentPositive = 0
		 --AND pc.claimStatus not in ('22') --AND pc.claimStatusOrig is not NULL
   GROUP BY pc.hcClaimId
),
CTE_Calculations AS (
   SELECT DISTINCT pc.hcClaimId,
    SUM(COALESCE(pc.totalPayment, 0)) AS TotalPayment,
	 SUM(COALESCE(pc.patientRespAmount, 0)) AS PatientRespAmount
   FROM [dbo].[Paymentclaim] pc 
   INNER JOIN [dbo].[Payment] p on pc.hcPaymentId = p.id
   WHERE (p.hcPaymentTypeId = 1 OR p.hcPaymentTypeId = 2) AND pc.dateDeleted IS NULL
   --AND pc.claimStatus not in ('22')
   GROUP BY pc.hcClaimId
),
CTE_ClaimRenderingProvider AS
(
   SELECT
        cce.hcClaimId,
        CASE
            WHEN COUNT(DISTINCT cce.RenderingProviderId) = 1 
                 THEN MAX(cce.RenderingProviderId)
            ELSE NULL
        END AS RenderingProviderId
    FROM dbo.ClaimChargeEntries cce
    WHERE cce.DateDeleted IS NULL
      AND cce.RenderingProviderId IS NOT NULL
    GROUP BY cce.hcClaimId
),
CTE_WriteOff AS (
   SELECT DISTINCT cw.ClaimId,
   SUM(ccew.writeOffAmount) AS WriteOffAmount
   FROM [dbo].[ClaimWriteOff] cw
   INNER JOIN [dbo].[ClaimChargeEntryWriteOff] ccew ON cw.Id = ccew.ClaimWriteOffId
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
where (cv.refvalidationid is null or cv.refvalidationid = 0)
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
	  renderingProviderSearchClaim.id AS RenderingProviderId,
	  crp.RenderingProviderId AS RenderingProviderId2,
	   crpSearch.name AS RenderingProviderName,
      cce3.Charges AS BilledAmount,
      ISNULL(cce3.Charges, 0) AS ExpectedAmount,
	  CASE WHEN c.hcClaimStatusId not in (5) THEN
		ISNULL(calcNew.TotalPayment, 0) ELSE 0 END AS PaymentAmount,
	  CASE 
		WHEN c.hcClaimStatusId not in (5) THEN
		  ISNULL(cce3.Charges, 0) - ISNULL(calcNew.TotalPayment, 0)
			  + ISNULL(calc.PositiveAdjustment, 0)
			  - ISNULL(calc1.NegativeAdjustment, 0)
			  - ISNULL(calc2.WriteOffAmount,0)
			  + (ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0)) 
		ELSE 
			ISNULL(cce3.Charges, 0) - ISNULL(calc2.WriteOffAmount,0) ---ISNULL(calcNew.TotalPayment, 0)
		END AS BalanceAmount,
	  CASE WHEN c.hcClaimStatusId not in (5) THEN
		ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) ELSE 0 END AS AdjustmentAmount,
	  CASE WHEN c.hcClaimStatusId not in (5) THEN
		ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0) ELSE 0 END AS PatientResponsibilityAmount,
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
	  c.primaryFunderId,
	  c.secondaryFunderId,	
	  c.IsSecondaryPayerAvailable,
	  funderSearchClaim.id AS FunderId,
	  CONCAT(clientSearchClaim.firstName, ISNULL(' '+ clientSearchClaim.middleName + ' ', ' '), clientSearchClaim.lastName) as patientName,
      cvec.ErrorsCount,
      cvec.WarningsCount,
	  ccrc.RespCount as ResponseCount,
      CAST(CEILING(CAST(cce3.NoOfCharges AS float) / 6) AS int) AS CMSPagesCount, -- For CMS1500 (Max. 6 charges allowed per CMS1500)
      COALESCE(locationCodes.Name, '') AS PlaceOfService,
      c.clientFunderId AS ChildProfileFunderId,
      funderSearchClaim.name AS FunderName,
	
	  rcdc.ReasonCodes as ReasonCodes,
	  cfrm.reasonName as Reason,
	  cft.comment as Comment,
	  cft.id as FlagReasonTransactionId,
	  cft.reasonId as ReasonId,						 
      CAST(CASE
             WHEN cn.hcClaimId IS NOT NULL THEN 1
             ELSE 0
          END AS BIT) AS HasNote,
      c.billedDate AS BilledDate,
      c.AssigneeId AS AssigneeId
	
INTO #temp
FROM [dbo].[Claims] AS c
LEFT JOIN CTE_ReasonCodeForDeniedClaim As rcdc ON rcdc.hcClaimId = c.id
LEFT JOIN CTE_PositiveAdjustments AS calc ON calc.hcClaimId = c.Id
LEFT JOIN CTE_NegativeAdjustments AS calc1 ON calc1.hcClaimId = c.Id
LEFT JOIN CTE_PositivePatientResponsibility AS calpr1 ON calpr1.hcClaimId = c.Id
LEFT JOIN CTE_NegativePatientResponsibility AS calpr2 ON calpr2.hcClaimId = c.Id
LEFT JOIN CTE_WriteOff AS calc2 ON calc2.ClaimId = c.Id
LEFT JOIN CTE_Calculations AS calcNew ON calcNew.hcClaimId = c.Id
LEFT JOIN CTE_Charges AS cce3 ON c.Id = cce3.hcClaimId
LEFT JOIN CTE_ResponseCount AS ccrc ON ccrc.ClaimId = c.id
LEFT JOIN [ClaimChargeEntries] AS cce ON c.Id = cce.hcClaimId
LEFT JOIN [ClaimAppointmentLink] AS cal ON c.Id = cal.hcClaimId
LEFT JOIN [ClaimStatus] AS cs ON c.hcClaimStatusId = cs.Id
LEFT JOIN [ClaimFlagTransaction] AS cft ON c.id = cft.hcClaimId and cft.datedeleted is null
LEFT JOIN [ClaimFlagReasonMaster] AS cfrm ON cft.reasonId = cfrm.id		
LEFT JOIN CTE_ClaimRenderingProvider crp
    ON crp.hcClaimId = c.Id

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
LEFT JOIN ClaimSearchFunders AS funderSearchClaim ON funderSearchClaim.Id = (SELECT TOP 1 Id FROM ClaimSearchFunders WHERE Id IN (childProfileAuths.funderId, c.primaryFunderId))
LEFT JOIN ClaimSearchRenderingProviders AS renderingProviderSearchClaim ON renderingProviderSearchClaim.Id = CASE
                                                                                                             WHEN c.renderingStaffMemberId = -2 THEN c.tblMemberId
                                                                                                             WHEN c.renderingStaffMemberId IS NULL THEN c.tblMemberId
                                                                                                             ELSE c.renderingStaffMemberId
                                                                                                         END AND renderingProviderSearchClaim.dateDeleted IS NULL

LEFT JOIN ClaimSearchRenderingProviders crpSearch
    ON crpSearch.Id = crp.RenderingProviderId

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
	((@Tab = 1 AND ([c].[hcClaimStatusId] = 1 OR [c].[hcClaimStatusId] = 20) AND [c].[isFlagged] = 0 OR 
	(@Tab = 2 AND ([c].[hcClaimStatusId] = 2 OR [c].[hcClaimStatusId] = 7 OR [c].[hcClaimStatusId] = 10 OR [c].[hcClaimStatusId] = 14 OR [c].[hcClaimStatusId] = 19) AND [c].[isFlagged] = 0) OR
	(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 11 OR [c].[hcClaimStatusId] = 12 OR [c].[hcClaimStatusId] = 15 OR [c].[hcClaimStatusId] = 16 OR [c].[hcClaimStatusId] = 17) AND [c].[isFlagged] = 0) OR
	(@Tab = 4 AND ((@ShowVoided = 1 AND [c].[hcClaimStatusId] = 18) OR [c].[hcClaimStatusId] = 6 OR [c].[hcClaimStatusId] = 13) AND [c].[isFlagged] = 0) OR
	(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
	(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
	(@Tab = 7 AND ([c].[isFlagged] = 1))))
		AND [c].[ClaimIdentifier] LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END

	AND (@PatientIds is null OR @PatientIds = '' OR [c].[childProfileId] in (select * from STRING_SPLIT(@PatientIds, ',')))

	AND (@ClaimIds is null OR @ClaimIds = '' OR [c].[id] in (select * from STRING_SPLIT(@ClaimIds, ',')))
	
	AND (@ReasonIds is null OR @ReasonIds = '' OR [cft].[reasonId] in (select * from STRING_SPLIT(@ReasonIds, ',')))

	AND (@FunderIds is null OR @FunderIds = '' OR [funderSearchClaim].[id] in (select * from STRING_SPLIT(@FunderIds, ',')))
	
	AND (@AssigneeIds is null OR @AssigneeIds = '' OR [c].[AssigneeId] in (select * from STRING_SPLIT(@AssigneeIds, ',')))  

	AND (@LocationIds IS NULL OR @LocationIds = '' OR [c].[hcProviderLocationId] IN (SELECT value FROM STRING_SPLIT(@LocationIds, ',')))

	AND ( @RenderingProviderIds IS NULL  OR @RenderingProviderIds = ''
				OR (
					-- Claim-level match
					renderingProviderSearchClaim.id IN (
						SELECT TRY_CAST(value AS INT)
						FROM STRING_SPLIT(@RenderingProviderIds, ',')
					)
					-- OR charge-level match
					OR EXISTS (
						SELECT 1
						FROM dbo.ClaimChargeEntries cceFilter
						WHERE cceFilter.hcClaimId = c.Id
						  AND cceFilter.DateDeleted IS NULL
						  AND cceFilter.RenderingProviderId IN (
								SELECT TRY_CAST(value AS INT)
								FROM STRING_SPLIT(@RenderingProviderIds, ',')
						  )
					)
    ))
	--OR [renderingProviderSearchClaim].[id] in (select * from STRING_SPLIT(@RenderingProviderIds, ',')))

	AND (@BalanceFrom IS NULL OR @BalanceFrom <= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) + ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))												 

	AND (@BalanceTo IS NULL OR @BalanceTo >= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) + ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))

	AND (@PatientResponsibilityFrom IS NULL OR @PatientResponsibilityFrom <= ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))
    AND (@PatientResponsibilityTo IS NULL OR @PatientResponsibilityTo >= ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))

	AND (@DateOfServiceFrom IS NULL OR c.startDate >= @DateOfServiceFrom)
    AND (@DateOfServiceTo IS NULL OR c.startDate <= @DateOfServiceTo)

	AND (@BilledFrom IS NULL OR @BilledFrom <= [cce3].[Charges])
    AND (@BilledTo IS NULL OR @BilledTo >= [cce3].[Charges])

	AND (@StatusIds IS NULL OR @StatusIds = '' OR [c].[hcClaimStatusId] IN (SELECT * FROM STRING_SPLIT(@StatusIds, ',')))

	--AND ((@ValidationIds IS NULL OR @ValidationIds = '' OR cem.severity IN
	--	(SELECT * FROM STRING_SPLIT(@ValidationIds, ',')) OR
	--		(cvec.WarningsCount IS NULL OR cvec.ErrorsCount IS NULL OR @ShowWithNoErrorsIndicator IN
	--			(SELECT * FROM STRING_SPLIT(@ValidationIds, ','))
	--	    )
	--	)

	--OR (@ResponseIds IS NULL OR @ResponseIds = '' OR ccrc.RespCount > 0))
	--AND (
	--	@ReasonCode IS NULL
	--	OR @ReasonCode = ''
	--	OR EXISTS (
	--		SELECT 1
	--		FROM STRING_SPLIT(@ReasonCode, ',') AS rc
	--		WHERE EXISTS (
	--			SELECT 1
	--			FROM STRING_SPLIT(rcdc.ReasonCodes, ',') AS rcdcCode
	--			WHERE LTRIM(RTRIM(rcdcCode.value)) = LTRIM(RTRIM(rc.value))
	--		)
	--	)
	--)

	AND (
    @ValidationIds IS NULL
    OR @ValidationIds = ''
    OR EXISTS (
        SELECT 1
        FROM dbo.ClaimValidationErrors cv2
        LEFT JOIN dbo.ClaimErrorMessages cem2 ON cem2.Id = cv2.claimErrorMessageId
        WHERE cv2.dateDeleted IS NULL
          AND cv2.claimId = c.Id
          AND TRY_CAST(LTRIM(RTRIM(cem2.severity)) AS INT) IN (
              SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) FROM STRING_SPLIT(@ValidationIds, ',')
          )
    )
    -- support for "show with no errors" indicator (if you still want it)
    OR (@ShowWithNoErrorsIndicator IS NOT NULL 
        AND @ValidationIds IS NOT NULL 
        AND CHARINDEX(CAST(@ShowWithNoErrorsIndicator AS varchar(10)), @ValidationIds) > 0
        AND NOT EXISTS (
            SELECT 1 FROM dbo.ClaimValidationErrors cv3 WHERE cv3.claimId = c.Id AND cv3.dateDeleted IS NULL
        )
    )
)

-- ===== Response filter (apply independently) =====
AND (
    @ResponseIds IS NULL
    OR @ResponseIds = ''
    -- if ResponseIds provided, check if this claim has any ClearingHouseResponseDetails rows with those response ids
    OR EXISTS (
        SELECT *
        FROM dbo.ClearingHouseResponseDetails ch
        WHERE ch.ClaimId = c.Id
          AND CAST(LTRIM(RTRIM(@ResponseIds)) AS VARCHAR(100)) IN (
              SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@ResponseIds, ',')
          )
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
		case when @OrderField = 'RenderingProviderName' and @OrderDir = 1 then c.RenderingProviderName end desc,

		case when @OrderField = 'BilledDate' and @OrderDir = 0 then c.BilledDate end asc,
		case when @OrderField = 'BilledDate' and @OrderDir = 1 then c.BilledDate end desc

	OFFSET @Skip ROWS FETCH NEXT CASE WHEN @Take = 0 THEN 2147483647 ELSE @Take END ROWS ONLY;

	drop table #temp
END
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

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ChargeTransactions') AND type in (N'U'))
BEGIN
	--Script to drop table [ChargeTransactions]
	DROP TABLE [dbo].[ChargeTransactions]
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ClaimTransactions') AND type in (N'U'))
BEGIN
	--Script to drop table [ClaimTransactions]
	DROP TABLE [dbo].[ClaimTransactions]
END
GO

/****** Object:  StoredProcedure [dbo].[GetClaimsByAccountInfoId]    Script Date: 11/13/2024 9:34:53 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- CHANGES FOR PATIENT RESPONSIBILITY
ALTER PROCEDURE [dbo].[GetClaimsByAccountInfoId]
--EXEC [dbo].[GetClaimsByAccountInfoId] 18421,0,50,'',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,2,0,NULL
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
	@ValidationIds nvarchar(100) = null,
	@ResponseIds nvarchar(100) = null
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

CTE_PositivePatientResponsibility AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS PositivePatientResponsibility
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON
pc.id
= pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON
pcsl.id
= pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode = 'PR'
         AND pcladj.IsAdjustmentPositive = 1
   GROUP BY pc.hcClaimId
),
CTE_NegativePatientResponsibility AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(pcladj.adjustmentAmount) AS NegativePatientResponsibility
   FROM [dbo].[Paymentclaim] pc
   INNER JOIN [dbo].[PaymentClaimServiceLine] pcsl ON
pc.id
= pcsl.hcPaymentClaimId
   INNER JOIN [dbo].[PaymentClaimServiceLineAdjustment] pcladj ON
pcsl.id
= pcladj.hcPaymentClaimServiceLineId
   WHERE pc.dateDeleted IS NULL
         AND pcladj.dateDeleted IS NULL
         AND pcladj.adjustmentGroupCode = 'PR'
         AND pcladj.IsAdjustmentPositive = 0
   GROUP BY pc.hcClaimId
),
CTE_Calculations AS (
   SELECT DISTINCT pc.hcClaimId,
          SUM(COALESCE(pc.totalPayment, 0)) AS TotalPayment,
          SUM(COALESCE(pc.patientRespAmount, 0)) AS PatientRespAmount
   FROM [dbo].[Paymentclaim] pc WHERE pc.dateDeleted IS NULL
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
	  renderingProviderSearchClaim.id AS RenderingProviderId,
      cce3.Charges AS BilledAmount,
      ISNULL(cce3.Charges, 0) AS ExpectedAmount,
      ISNULL(calcNew.TotalPayment, 0) AS PaymentAmount,
      ISNULL(cce3.Charges, 0) - ISNULL(calcNew.TotalPayment, 0)
          + ISNULL(calc.PositiveAdjustment, 0)
          - ISNULL(calc1.NegativeAdjustment, 0)
		  - ISNULL(calc2.WriteOffAmount,0)
          + (ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0)) AS BalanceAmount,
      ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) AS AdjustmentAmount,
	  ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0) AS PatientResponsibilityAmount,
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
	  CONCAT(clientSearchClaim.firstName, ISNULL(' '+ clientSearchClaim.middleName + ' ', ' '), clientSearchClaim.lastName) as patientName,
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
LEFT JOIN CTE_PositivePatientResponsibility AS calpr1 ON calpr1.hcClaimId = c.Id
LEFT JOIN CTE_NegativePatientResponsibility AS calpr2 ON calpr2.hcClaimId = c.Id
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
	(@Tab = 3 AND ([c].[hcClaimStatusId] = 3 OR [c].[hcClaimStatusId] = 4 OR [c].[hcClaimStatusId] = 12 OR [c].[hcClaimStatusId] = 15 OR [c].[hcClaimStatusId] = 16 OR [c].[hcClaimStatusId] = 17) AND [c].[isFlagged] = 0) OR
	(@Tab = 4 AND ((@ShowVoided = 1 AND [c].[hcClaimStatusId] = 18) OR [c].[hcClaimStatusId] = 6 OR [c].[hcClaimStatusId] = 13) AND [c].[isFlagged] = 0) OR
	(@Tab = 5 AND ([c].[hcClaimStatusId] = 8 OR [c].[hcClaimStatusId] = 9) AND [c].[isFlagged] = 0) OR
	(@Tab = 6 AND ([c].[hcClaimStatusId] = 5) AND [c].[isFlagged] = 0) OR
	(@Tab = 7 AND ([c].[isFlagged] = 1))))
		AND [c].[ClaimIdentifier] LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END

	AND (@PatientIds is null OR @PatientIds = '' OR [clientSearchClaim].[id] in (select * from STRING_SPLIT(@PatientIds, ',')))

	AND (@ClaimIds is null OR @ClaimIds = '' OR [c].[id] in (select * from STRING_SPLIT(@ClaimIds, ',')))

	AND (@FunderIds is null OR @FunderIds = '' OR [funderSearchClaim].[id] in (select * from STRING_SPLIT(@FunderIds, ',')))

	AND (@RenderingProviderIds is null OR @RenderingProviderIds = '' OR [renderingProviderSearchClaim].[id] in (select * from STRING_SPLIT(@RenderingProviderIds, ',')))

	AND (@BalanceFrom IS NULL OR @BalanceFrom <= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) + ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))												 

	AND (@BalanceTo IS NULL OR @BalanceTo >= ISNULL([cce3].[Charges], 0) - ISNULL([calcNew].[totalPayment], 0) + ISNULL(calc.PositiveAdjustment, 0) - ISNULL(calc1.NegativeAdjustment, 0) - ISNULL(calc2.WriteOffAmount,0) + ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))

	AND (@PatientResponsibilityFrom IS NULL OR @PatientResponsibilityFrom <= ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))
    AND (@PatientResponsibilityTo IS NULL OR @PatientResponsibilityTo >= ISNULL(calpr1.PositivePatientResponsibility, 0) - ISNULL(calpr2.NegativePatientResponsibility, 0))

	AND (@BilledFrom IS NULL OR @BilledFrom <= [cce3].[Charges])
    AND (@BilledTo IS NULL OR @BilledTo >= [cce3].[Charges])

	AND (@StatusIds IS NULL OR @StatusIds = '' OR [c].[hcClaimStatusId] IN (SELECT * FROM STRING_SPLIT(@StatusIds, ',')))

	AND (@ValidationIds IS NULL OR @ValidationIds = '' OR cem.severity IN
		(SELECT * FROM STRING_SPLIT(@ValidationIds, ',')) OR
			(cvec.WarningsCount IS NULL AND cvec.ErrorsCount IS NULL AND @ShowWithNoErrorsIndicator IN
				(SELECT * FROM STRING_SPLIT(@ValidationIds, ','))
		    )
		)

	AND (@ResponseIds IS NULL OR @ResponseIds = '' OR ccrc.RespCount > 0)

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
GO

ALTER PROCEDURE [dbo].[GetPatientInvoiceCreationDetails]
(
   @AccountInfoId INT,
   @ClientIds NVARCHAR(MAX) = NULL,
   @PatientResponsibilityFrom DECIMAL(18, 2) = 0.0,
   @PatientResponsibilityTo DECIMAL(18, 2) = 0.0,
   @DateFrom DATETIME = NULL,
   @DateTo DATETIME = NULL
)
AS
BEGIN
   SET NOCOUNT ON;
   DECLARE @ClientIdTable TABLE (ClientId INT);
   IF @ClientIds IS NOT NULL AND @ClientIds != ''
   BEGIN
       INSERT INTO @ClientIdTable (ClientId)
       SELECT DISTINCT TRY_CAST(value AS INT)
       FROM STRING_SPLIT(@ClientIds, ',')
       WHERE TRIM(value) IS NOT NULL;
   END
   ;WITH NonPatientAdjustments AS (
       SELECT
           pcsa.hcPaymentClaimServiceLineId,
           SUM(CASE
               WHEN pcsa.adjustmentGroupCode != 'PR' THEN
                   CASE
                       WHEN pcsa.isAdjustmentPositive = 1 THEN pcsa.AdjustmentAmount
                       ELSE -pcsa.AdjustmentAmount
                   END
               ELSE 0
           END) AS NonPatientAdjustmentTotal
       FROM
           PaymentClaimServiceLineAdjustment pcsa
       WHERE
           pcsa.dateDeleted IS NULL
       GROUP BY
           pcsa.hcPaymentClaimServiceLineId
   ),
   PatientAdjustments AS (
       SELECT
           pcsaPR.hcPaymentClaimServiceLineId,
           SUM(CASE
               WHEN pcsaPR.adjustmentGroupCode = 'PR' THEN
                   CASE
                       WHEN pcsaPR.isAdjustmentPositive = 1 THEN -pcsaPR.AdjustmentAmount
                       ELSE pcsaPR.AdjustmentAmount
                   END
               ELSE 0
           END) AS PatientAdjustmentTotal
       FROM
           PaymentClaimServiceLineAdjustment pcsaPR
       WHERE
           pcsaPR.dateDeleted IS NULL
       GROUP BY
           pcsaPR.hcPaymentClaimServiceLineId
   ),
   Writeoff AS(
		SELECT DISTINCT ClaimChargeEntryId, SUM(writeOffAmount) as  writeOffAmount
		FROM ClaimChargeEntryWriteOff 
		WHERE dateDeleted IS NULL
		GROUP BY ClaimChargeEntryId
   )
   SELECT
       cc.id AS Id,
	   c.id AS ClaimId,
       c.childProfileId AS ClientId,
       CONCAT(client.firstName, ISNULL(' ' + client.middleName + ' ', ' '), client.lastName) AS ClientName,
       cc.BillingCode,
       CONVERT(varchar, cc.DateOfService, 101) AS DateOfService,
       CAST(ROUND(cc.units, 2) AS DECIMAL(18, 2)) AS Units,
       CAST(ROUND(cc.Charges, 2) AS DECIMAL(18, 2)) AS Charges,
       SUM(CASE WHEN paytyp.id in (1,2) THEN pcsl.paymentAmount ELSE 0 END) AS InsuranceAmount,
       ISNULL(SUM(npa.NonPatientAdjustmentTotal), 0) - ISNULL(wo.writeOffAmount, 0) AS Adjustment_Non_Patient_responsibility,
       ISNULL(SUM(pa.PatientAdjustmentTotal), 0) AS Adjustment_Patient_responsibility,
       SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) AS PatientAmount,
       ISNULL(SUM(pa.PatientAdjustmentTotal), 0) - SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) AS PatientBalance,
       (SELECT TOP 1 name FROM patientinvoicestatus WHERE id = 1 AND dateDeleted IS NULL) AS InvoiceStatus
	  
   FROM
       Claims c
   JOIN
       PaymentClaim paycl ON c.id = paycl.hcClaimId
   JOIN
       ClaimChargeEntries cc ON paycl.hcClaimId = cc.hcClaimId  
   JOIN
       PaymentClaimServiceLine pcsl ON cc.Id = pcsl.hcClaimChargeEntryId AND paycl.id = pcsl.hcPaymentClaimId   
   JOIN
       Payment pay ON paycl.hcPaymentId = pay.Id
   JOIN
       PaymentType paytyp ON pay.hcPaymentTypeId = paytyp.Id
   JOIN
       ClaimSearchClients AS client ON client.Id = c.childProfileId
   LEFT JOIN
		Writeoff wo ON cc.id = wo.ClaimChargeEntryId
   LEFT JOIN 
		NonPatientAdjustments npa ON pcsl.Id = npa.hcPaymentClaimServiceLineId
   LEFT JOIN 
		PatientAdjustments pa ON pcsl.Id = pa.hcPaymentClaimServiceLineId

   WHERE	  
       c.accountInfoId = @AccountInfoId
       AND c.dateDeleted IS NULL
       AND cc.dateDeleted IS NULL
       AND pcsl.dateDeleted IS NULL
       AND paycl.dateDeleted IS NULL
       AND pay.dateDeleted IS NULL
       AND paytyp.dateDeleted IS NULL
       AND paytyp.id IN (1,2,3)
       AND (@ClientIds IS NULL OR @ClientIds = '' OR EXISTS (SELECT 1 FROM @ClientIdTable WHERE ClientId = c.childProfileId))
       AND (@DateFrom IS NULL OR cc.DateOfService >= @DateFrom)
       AND (@DateTo IS NULL OR cc.DateOfService <= @DateTo)
       AND NOT EXISTS (
           SELECT 1
           FROM PatientInvoiceDetails pid
           WHERE pid.ClaimChargeEntryId = cc.Id
       )
   GROUP BY
	   cc.Id
	   ,wo.ClaimChargeEntryId
	   ,c.id,	   
       c.childProfileId,
       client.firstName,
       client.middleName,
       client.lastName,
       cc.BillingCode,
       cc.DateOfService,       
       cc.units,
       cc.Charges	   
	   ,wo.writeOffAmount
   HAVING
       ISNULL(SUM(pa.PatientAdjustmentTotal), 0) != 0
       AND ISNULL(SUM(pa.PatientAdjustmentTotal), 0) - SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) != 0
       AND (@PatientResponsibilityFrom IS NULL OR @PatientResponsibilityFrom = 0.0 OR
           ISNULL(SUM(pa.PatientAdjustmentTotal), 0) - SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) >= @PatientResponsibilityFrom)
       AND (@PatientResponsibilityTo IS NULL OR @PatientResponsibilityTo = 0.0 OR
           ISNULL(SUM(pa.PatientAdjustmentTotal), 0) - SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) <= @PatientResponsibilityTo)
	   AND 
		  ISNULL(SUM(pa.PatientAdjustmentTotal), 0) - SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) != 0
   ORDER BY
       cc.DateOfService;
END
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
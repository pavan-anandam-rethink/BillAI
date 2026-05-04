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

/****** Object:  StoredProcedure [dbo].[GetPatientInvoiceCreationDetails]    Script Date: 11/5/2024 5:23:27 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:      Navnath
-- Create Date: 30-Aug-2024
-- Description: To fetch the details for patient invoice creation.
-- EXEC GetPatientInvoiceCreationDetails_ERA 18421,'',1000,2000,null,null
-- EXEC GetPatientInvoiceCreationDetails_ERA 18421,'',0.0,0.0,null,null
-- =============================================
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
       cc.DateOfService,
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
		  ISNULL(SUM(pa.PatientAdjustmentTotal), 0) - SUM(CASE WHEN paytyp.id = 3 THEN pcsl.paymentAmount ELSE 0 END) >= 0
   ORDER BY
       cc.DateOfService;
END



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
-- =============================================
-- Corrected: usp_CalculateStartingAR
-- Purpose: Calculate Prior Period Balance group by funder (as of day BEFORE StartDate)
-- Used by: usp_FunderFinancialSummary
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_CalculateFunderStartingAR]
(
    @AccountInfoId INT,
    @StartDate DATE,
    @DateBasis NVARCHAR(20) = 'Transaction', -- Transaction | Deposit
    @LocationIds NVARCHAR(MAX) = NULL,
    @FunderIds NVARCHAR(MAX) = NULL,
    @RenderingProviderIds NVARCHAR(MAX) = NULL,
    @BillingProviderIds NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
 
    ------------------------------------------------------------
    -- As-Of Boundary (11:59:59 PM day before StartDate)
    ------------------------------------------------------------
    DECLARE @AsOfDateTime DATETIME =
        DATEADD(SECOND, -1, CAST(@StartDate AS DATETIME));
 
    ------------------------------------------------------------
    -- Eligible Claims (Single Source of Truth)
    ------------------------------------------------------------
    ;WITH EligibleClaims AS (
        SELECT
            c.id AS ClaimId,
            c.primaryFunderId AS FunderId
        FROM Claims c
        LEFT JOIN ClaimSubmissions cs ON cs.hcClaimId = c.id
        WHERE c.accountinfoid = @AccountInfoId
          AND c.dateDeleted IS NULL
          AND c.hcClaimStatusId NOT IN (2, 18)
 
          AND (
                @LocationIds IS NULL OR LTRIM(RTRIM(@LocationIds)) = ''
                OR c.hcProviderLocationId IN (
                    SELECT TRY_CAST(value AS INT)
                    FROM STRING_SPLIT(@LocationIds, ',')
                    WHERE ISNUMERIC(value) = 1
                )
          )
          AND (
                @FunderIds IS NULL OR LTRIM(RTRIM(@FunderIds)) = ''
                OR c.primaryFunderId IN (
                    SELECT TRY_CAST(value AS INT)
                    FROM STRING_SPLIT(@FunderIds, ',')
                    WHERE ISNUMERIC(value) = 1
                )
          )
          AND (
                @RenderingProviderIds IS NULL OR LTRIM(RTRIM(@RenderingProviderIds)) = ''
                OR c.renderingStaffMemberId IN (
                    SELECT TRY_CAST(value AS INT)
                    FROM STRING_SPLIT(@RenderingProviderIds, ',')
                    WHERE ISNUMERIC(value) = 1
                )
          )
          AND (
                @BillingProviderIds IS NULL OR LTRIM(RTRIM(@BillingProviderIds)) = ''
                OR EXISTS (
                    SELECT 1
                    FROM ClaimSubmissions cs2
                    WHERE cs2.hcClaimId = c.id
                      AND cs2.dateDeleted IS NULL
                      AND cs2.locationBillingProviderNpiNumber IN (
                          SELECT LTRIM(RTRIM(value))
                          FROM STRING_SPLIT(@BillingProviderIds, ',')
                          WHERE LTRIM(RTRIM(value)) <> ''
                      )
                )
          )
    ),
 
    ------------------------------------------------------------
    -- Charges
    ------------------------------------------------------------
    ChargeAgg AS (
        SELECT
            ec.FunderId,
            SUM(ch.charges) AS Charges
        FROM ClaimChargeEntries ch
        JOIN EligibleClaims ec ON ch.hcClaimId = ec.ClaimId
        WHERE ch.dateDeleted IS NULL
          AND ch.dateOfService <= @AsOfDateTime
        GROUP BY ec.FunderId
    ),
 
    ------------------------------------------------------------
    -- Insurance Payments
    ------------------------------------------------------------
    InsurancePayments AS (
        SELECT
            ec.FunderId,
            SUM(pc.totalPayment) AS InsurancePay
        FROM PaymentClaim pc
        JOIN Payment p ON pc.hcPaymentId = p.id
        JOIN EligibleClaims ec ON pc.hcClaimId = ec.ClaimId
        WHERE p.hcPaymentTypeId IN (1,2)
          AND pc.claimStatus NOT IN (22)
          AND p.dateDeleted IS NULL
          AND (CASE WHEN @DateBasis = 'Deposit'
                    THEN p.depositDate
                    ELSE p.postDate
               END) <= @AsOfDateTime
        GROUP BY ec.FunderId
    ),
 
    ------------------------------------------------------------
    -- Patient Payments
    ------------------------------------------------------------
    PatientPayments AS (
        SELECT
            ec.FunderId,
            SUM(pc.totalPayment) AS PatientPay
        FROM PaymentClaim pc
        JOIN Payment p ON pc.hcPaymentId = p.id
        JOIN EligibleClaims ec ON pc.hcClaimId = ec.ClaimId
        WHERE p.hcPaymentTypeId = 3
          AND p.dateDeleted IS NULL
          AND (CASE WHEN @DateBasis = 'Deposit'
                    THEN p.depositDate
                    ELSE p.postDate
               END) <= @AsOfDateTime
        GROUP BY ec.FunderId
    ),
 
    ------------------------------------------------------------
    -- Adjustments (Non-PR)
    ------------------------------------------------------------
    Adjustments AS (
        SELECT
            ec.FunderId,
            SUM(
                CASE
                    WHEN adj.IsAdjustmentPositive = 1
                        THEN adj.adjustmentAmount * -1
                    ELSE adj.adjustmentAmount
                END
            ) AS Adjustments
        FROM PaymentClaim pc
        JOIN Payment p ON pc.hcPaymentId = p.id
        JOIN PaymentClaimServiceLine pcl ON pcl.hcPaymentClaimId = pc.id
        JOIN PaymentClaimServiceLineAdjustment adj
            ON adj.hcPaymentClaimServiceLineId = pcl.id
        JOIN EligibleClaims ec ON pc.hcClaimId = ec.ClaimId
        WHERE adj.dateDeleted IS NULL
          AND adj.adjustmentGroupCode <> 'PR'
          AND p.dateDeleted IS NULL
          AND (CASE WHEN @DateBasis = 'Deposit'
                    THEN p.depositDate
                    ELSE p.postDate
               END) <= @AsOfDateTime
        GROUP BY ec.FunderId
    ),
 
    ------------------------------------------------------------
    -- Write-Offs
    ------------------------------------------------------------
    WriteOffs AS (
        SELECT
            ec.FunderId,
            SUM(wo.writeOffAmount) AS WriteOffs
        FROM ClaimChargeEntryWriteOff wo
        JOIN ClaimChargeEntries ch ON ch.id = wo.ClaimChargeEntryId
        JOIN EligibleClaims ec ON ch.hcClaimId = ec.ClaimId
        WHERE wo.dateDeleted IS NULL
          AND wo.dateCreated <= @AsOfDateTime
        GROUP BY ec.FunderId
    )
 
    ------------------------------------------------------------
    -- Final Funder-wise Starting A/R
    ------------------------------------------------------------
    SELECT
        f.FunderId,
        COALESCE(c.Charges, 0)
        - COALESCE(i.InsurancePay, 0)
        - COALESCE(p.PatientPay, 0)
        - COALESCE(a.Adjustments, 0)
        - COALESCE(w.WriteOffs, 0) AS StartingAR
    FROM (SELECT DISTINCT FunderId FROM EligibleClaims) f
    LEFT JOIN ChargeAgg c ON c.FunderId = f.FunderId
    LEFT JOIN InsurancePayments i ON i.FunderId = f.FunderId
    LEFT JOIN PatientPayments p ON p.FunderId = f.FunderId
    LEFT JOIN Adjustments a ON a.FunderId = f.FunderId
    LEFT JOIN WriteOffs w ON w.FunderId = f.FunderId
    ORDER BY f.FunderId;
 
END;
GO

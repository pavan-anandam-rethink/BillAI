
-- =============================================
-- usp_FunderFinancialSummary
-- Purpose: Generate Funder Financial Summary Report
-- Returns: 
--   1. StartingAR (Prior Period Balance - Total across all funders)
--   2. Funder Rows + TOTAL row
--   3. Unapplied Credits
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_FunderFinancialSummary]       
(        
    @AccountInfoId INT,        
    @StartDate DATE,        
    @EndDate   DATE,        
    @DateBasis NVARCHAR(20) = 'Transaction',   -- Transaction | Deposit        
    @LocationIds VARCHAR(MAX) = NULL,          -- CSV        
    @FunderIds   VARCHAR(MAX) = NULL,          -- CSV        
    @RenderingProviderIds VARCHAR(MAX) = NULL, -- CSV (Optional)        
    @BillingProviderIds VARCHAR(MAX) = NULL    -- CSV (Optional)        
)        
AS        
BEGIN        
    SET NOCOUNT ON;        
        
     
    ------------------------------------------------------------
-- 1. Get Starting A/R by Funder
------------------------------------------------------------
DECLARE @StartingARByFunder TABLE (
    FunderId INT PRIMARY KEY,
    PriorPeriodBalance DECIMAL(18,2)
);

SELECT
    COALESCE(SUM(PriorPeriodBalance), 0) AS StartingARBalance
FROM @StartingARByFunder;

INSERT INTO @StartingARByFunder (FunderId, PriorPeriodBalance)
EXEC dbo.usp_CalculateFunderStartingAR
    @AccountInfoId = @AccountInfoId,
    @StartDate     = @StartDate,
    @DateBasis     = @DateBasis,
    @LocationIds   = @LocationIds,
    @FunderIds     = @FunderIds,
    @RenderingProviderIds = @RenderingProviderIds,
    @BillingProviderIds   = @BillingProviderIds;
    ------------------------------------------------------------        
    -- 2. Eligible Claims (claim-scoped)        
    ------------------------------------------------------------        
    ;WITH EligibleClaims AS (        
        SELECT         
            c.id AS ClaimId,        
            c.primaryFunderId AS FunderId        
        FROM Claims c        
        WHERE c.accountinfoid = @AccountInfoId    
    AND c.dateDeleted IS NULL         
          AND c.hcClaimStatusId NOT IN (2,18)        
                  
          -- Optional Location filter        
          AND (        
              @LocationIds IS NULL        
              OR LTRIM(RTRIM(@LocationIds)) = ''        
              OR c.hcProviderLocationId IN (        
                  SELECT TRY_CAST(value AS INT)         
                  FROM STRING_SPLIT(@LocationIds, ',')        
              )        
          )        
                  
          -- Optional Funder filter        
          AND (        
              @FunderIds IS NULL        
              OR LTRIM(RTRIM(@FunderIds)) = ''        
              OR c.primaryFunderId IN (        
                  SELECT TRY_CAST(value AS INT)         
                  FROM STRING_SPLIT(@FunderIds, ',')        
              )        
          )        
                  
          -- Optional Rendering Provider filter        
          AND (        
              @RenderingProviderIds IS NULL        
              OR LTRIM(RTRIM(@RenderingProviderIds)) = ''        
              OR c.RenderingStaffMemberId IN (        
                  SELECT TRY_CAST(value AS INT)         
                  FROM STRING_SPLIT(@RenderingProviderIds, ',')        
              )        
          )        
                  
          -- Optional Billing Provider filter        
                  AND (  
      @BillingProviderIds IS NULL  
      OR LTRIM(RTRIM(@BillingProviderIds)) = ''  
      OR EXISTS (  
      SELECT 1  
      FROM ClaimSubmissions cs  
      WHERE cs.hcClaimId = c.id        
                      AND cs.dateDeleted IS NULL  
          AND cs.locationBillingProviderNpiNumber IN (  
       SELECT LTRIM(RTRIM(value))  
          FROM STRING_SPLIT(@BillingProviderIds, ',')  
       WHERE LTRIM(RTRIM(value)) <> ''  
       )  
     )  
    )  
   ),    
        
    ------------------------------------------------------------        
    -- 3. Get Funder Information        
    ------------------------------------------------------------        
    Funders AS (        
        SELECT DISTINCT        
            ec.FunderId,        
            COALESCE(f.name, 'Unknown') AS FunderName        
        FROM EligibleClaims ec        
    LEFT JOIN ClaimSearchFunders f ON ec.FunderId = f.id        
    ),

    ------------------------------------------------------------        
    -- 5. Charges by Funder        
    ------------------------------------------------------------        
    Charges AS (        
        SELECT        
            ec.FunderId,        
            SUM(ch.charges) AS Charges        
        FROM ClaimChargeEntries ch        
        JOIN EligibleClaims ec ON ch.hcClaimId = ec.ClaimId
        WHERE ch.dateOfService >= @StartDate and ch.dateOfService < DATEADD(DAY, 1, @EndDate) and ch.DateDeleted is null        
        GROUP BY ec.FunderId     
    ),
        
    ------------------------------------------------------------        
    -- 6. Insurance Payments by Funder        
    ------------------------------------------------------------        
    InsurancePayments AS (        
        SELECT        
            ec.FunderId,        
            SUM(pc.totalPayment) AS InsurancePay        
        FROM PaymentClaim pc        
        JOIN Payment p ON pc.hcPaymentId = p.id        
        JOIN EligibleClaims ec ON pc.hcClaimId = ec.ClaimId        
        --WHERE p.hcPaymentTypeId IN (1,2)        
        --  AND (CASE WHEN @DateBasis = 'Deposit' THEN p.depositDate ELSE p.postDate END)         
        --      BETWEEN @StartDate AND @EndDate    
		WHERE p.hcPaymentTypeId IN (1,2) and pc.claimStatus not in (22)
          AND (
          (@DateBasis = 'Deposit' AND p.depositDate >= @StartDate AND p.depositDate < DATEADD(DAY, 1, @EndDate))
          OR
          (@DateBasis <> 'Deposit' AND p.postDate >= @StartDate AND p.postDate < DATEADD(DAY, 1, @EndDate))
         ) and p.dateDeleted is null
        GROUP BY ec.FunderId        
    ),        
        
    ------------------------------------------------------------        
    -- 7. Patient Payments by Funder        
    ------------------------------------------------------------        
    PatientPayments AS (        
        SELECT        
            ec.FunderId,        
            SUM(pc.totalPayment) AS PatientPay        
        FROM PaymentClaim pc        
        JOIN Payment p ON pc.hcPaymentId = p.id        
        JOIN EligibleClaims ec ON pc.hcClaimId = ec.ClaimId
		 WHERE p.hcPaymentTypeId = 3 and pc.claimStatus not in (22)
          AND (
          (@DateBasis = 'Deposit' AND p.depositDate >= @StartDate AND p.depositDate < DATEADD(DAY, 1, @EndDate))
          OR
          (@DateBasis <> 'Deposit' AND p.postDate >= @StartDate AND p.postDate < DATEADD(DAY, 1, @EndDate))
         ) and p.dateDeleted is null
        GROUP BY ec.FunderId        
    ),        
        
    ------------------------------------------------------------        
    -- 8. Adjustments by Funder        
    ------------------------------------------------------------        
    Adjustments AS (        
        SELECT        
            ec.FunderId,        
            SUM(        
                CASE WHEN adj.IsAdjustmentPositive = 1        
                     THEN adj.adjustmentAmount * -1       
                     ELSE adj.adjustmentAmount         
                END        
            ) AS Adjustments        
        FROM PaymentClaim pc        
        JOIN Payment p ON pc.hcPaymentId = p.id        
        JOIN EligibleClaims ec ON pc.hcClaimId = ec.ClaimId        
        JOIN PaymentClaimServiceLine pcl ON pcl.hcPaymentClaimId = pc.id        
        JOIN PaymentClaimServiceLineAdjustment adj ON adj.hcPaymentClaimServiceLineId = pcl.id
		 WHERE adj.dateDeleted IS NULL AND adj.adjustmentGroupCode <> 'PR'
          AND (
          (@DateBasis = 'Deposit' AND p.depositDate >= @StartDate AND p.depositDate < DATEADD(DAY, 1, @EndDate))
          OR
          (@DateBasis <> 'Deposit' AND p.postDate >= @StartDate AND p.postDate < DATEADD(DAY, 1, @EndDate))
         ) and p.dateDeleted is null and adj.dateDeleted is null
        GROUP BY ec.FunderId        
    ),        
        
    ------------------------------------------------------------        
    -- 9. Write-Offs by Funder        
    ------------------------------------------------------------        
    WriteOffs AS (        
        SELECT        
            ec.FunderId,        
   SUM(wo.writeOffAmount) AS WriteOffs        
        FROM ClaimChargeEntryWriteOff wo        
        JOIN ClaimChargeEntries ch ON wo.ClaimChargeEntryId = ch.id        
        JOIN EligibleClaims ec ON ch.hcClaimId = ec.ClaimId        
		WHERE wo.DateCreated >= @StartDate and wo.DateCreated < DATEADD(DAY, 1, @EndDate) and wo.dateDeleted is null
        GROUP BY ec.FunderId        
    ),        
        
    ------------------------------------------------------------        
    -- 10. Combine Funder Activity        
    ------------------------------------------------------------    
        
		FunderActivity AS (
    SELECT
        f.FunderId,
        f.FunderName,
        COALESCE(sa.PriorPeriodBalance, 0) AS PriorPeriodBalance,
        COALESCE(c.Charges, 0) AS Charges,
        COALESCE(ip.InsurancePay, 0) AS InsurancePay,
        COALESCE(pp.PatientPay, 0) AS PatientPay,
        COALESCE(ip.InsurancePay, 0) + COALESCE(pp.PatientPay, 0) AS TotalPay,
        COALESCE(a.Adjustments, 0) AS Adjustments,
        COALESCE(w.WriteOffs, 0) AS WriteOffs,
        COALESCE(c.Charges, 0)
          - (COALESCE(ip.InsurancePay, 0) + COALESCE(pp.PatientPay, 0))
          - COALESCE(a.Adjustments, 0)
          - COALESCE(w.WriteOffs, 0) AS PeriodBalance
    FROM Funders f
    LEFT JOIN @StartingARByFunder sa
        ON sa.FunderId = f.FunderId
    LEFT JOIN Charges c ON c.FunderId = f.FunderId
    LEFT JOIN InsurancePayments ip ON ip.FunderId = f.FunderId
    LEFT JOIN PatientPayments pp ON pp.FunderId = f.FunderId
    LEFT JOIN Adjustments a ON a.FunderId = f.FunderId
    LEFT JOIN WriteOffs w ON w.FunderId = f.FunderId
),

    ------------------------------------------------------------        
    -- 11. Calculate Total Balance        
    ------------------------------------------------------------        
    FinalRows AS (        
        SELECT        
            FunderId,        
            FunderName,        
            @DateBasis AS DateBasis,        
            PriorPeriodBalance,        
            Charges,        
            InsurancePay,        
            PatientPay,       
            TotalPay,        
            Adjustments,        
            WriteOffs,        
            PeriodBalance,
            PriorPeriodBalance + PeriodBalance AS TotalBalance        
        FROM FunderActivity        
    ),        
        
    ------------------------------------------------------------        
    -- 12. Generate Totals Row        
    ------------------------------------------------------------
Totals AS (        
    SELECT        
        NULL AS FunderId,        
        'TOTAL' AS FunderName,        
        @DateBasis AS DateBasis,        
        COALESCE(SUM(PriorPeriodBalance), 0) AS PriorPeriodBalance,        
        COALESCE(SUM(Charges), 0) AS Charges,        
        COALESCE(SUM(InsurancePay), 0) AS InsurancePay,        
        COALESCE(SUM(PatientPay), 0) AS PatientPay,        
        COALESCE(SUM(TotalPay), 0) AS TotalPay,        
        COALESCE(SUM(Adjustments), 0) AS Adjustments,        
        COALESCE(SUM(WriteOffs), 0) AS WriteOffs,        
        COALESCE(SUM(PeriodBalance), 0) AS PeriodBalance,        
        COALESCE(SUM(TotalBalance), 0) AS TotalBalance        
    FROM FinalRows        
)
        
    ------------------------------------------------------------        
    -- 13. Output: Main Report with Totals        
    ------------------------------------------------------------        
    SELECT *        
    FROM (        
        SELECT * FROM FinalRows        
        UNION ALL        
        SELECT * FROM Totals        
    ) r        
    ORDER BY        
        CASE WHEN FunderName = 'TOTAL' THEN 2 ELSE 1 END,        
        FunderName;        
        
    ------------------------------------------------------------        
   -- 14. Output: Unapplied Credits (separate result set)        
    ------------------------------------------------------------        
    ;WITH AppliedToEligibleClaims AS (
    SELECT
        pc.hcPaymentId,
        SUM(pc.totalPayment) AS AppliedAmount
    FROM PaymentClaim pc
    JOIN Claims c ON pc.hcClaimId = c.id
    WHERE c.dateDeleted IS NULL AND c.hcClaimStatusId NOT IN (2,18) AND c.accountinfoid = @AccountInfoId   

      -- Optional Location filter
      --AND (
      --      @LocationIds IS NULL
      --      OR LTRIM(RTRIM(@LocationIds)) = ''
      --      OR c.hcProviderLocationId IN (
      --          SELECT TRY_CAST(value AS INT)
      --          FROM STRING_SPLIT(@LocationIds, ',')
      --      )
      --    )

      ---- Optional Funder filter
      --AND (
      --      @FunderIds IS NULL
      --      OR LTRIM(RTRIM(@FunderIds)) = ''
      --      OR c.clientFunderId IN (
      --          SELECT TRY_CAST(value AS INT)
      --          FROM STRING_SPLIT(@FunderIds, ',')
      --      )
      --    )
    GROUP BY pc.hcPaymentId
),
AccountPayments AS (
    SELECT
        p.id AS PaymentId,
        p.hcPaymentTypeId,
        p.paymentAmount
    FROM Payment p
	--JOIN PaymentClaim pc on p.id=pc.hcpaymentid
    WHERE p.accountinfoid = @AccountInfoId   and p.dateDeleted is null
      AND (
            (@DateBasis = 'Deposit'
                AND p.depositDate >= @StartDate
                AND p.depositDate < DATEADD(DAY, 1, @EndDate))
         OR (@DateBasis <> 'Deposit'
                AND p.postDate >= @StartDate
                AND p.postDate < DATEADD(DAY, 1, @EndDate))
          )
),
PaymentUnapplied AS (
    SELECT
        ap.PaymentId,
        ap.hcPaymentTypeId,
        ap.paymentAmount - COALESCE(a.AppliedAmount, 0) AS UnappliedAmount
    FROM AccountPayments ap
    LEFT JOIN AppliedToEligibleClaims a
        ON a.hcPaymentId = ap.PaymentId
)
SELECT
    COALESCE(SUM(CASE WHEN hcPaymentTypeId IN (1,2) THEN UnappliedAmount ELSE 0 END), 0)
        AS InsuranceUnapplied,
    COALESCE(SUM(CASE WHEN hcPaymentTypeId = 3 THEN UnappliedAmount ELSE 0 END), 0)
        AS PatientUnapplied,
    COALESCE(SUM(UnappliedAmount), 0)
        AS TotalUnapplied
FROM PaymentUnapplied
WHERE UnappliedAmount > 0;       
        
END; 
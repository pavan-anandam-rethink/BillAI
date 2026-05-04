
/* ============================================================
   STEP 1 — Create table if not exists
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RHD_2011_Claims')
BEGIN
    PRINT 'Creating table RHD_2011_Claims...';

    CREATE TABLE dbo.RHD_2011_Claims
    (
        Id                         INT,
        AccountInfoId              INT,
        ClaimNumber                VARCHAR(50),
        AuthorizationNumber        VARCHAR(100),
        ChildProfileId             INT,
        ChildProfileAuthorizationId INT,
        LocationCodeId             INT,
        DateOfServiceStart         DATE,
        DateOfServiceEnd           DATE,
        BilledAmount               DECIMAL(18,2),
        ExpectedAmount             DECIMAL(18,2),
        BalanceAmount              DECIMAL(18,2),
        HcClaimStatusId            INT,
        Status                     VARCHAR(100),
        CreatedOn                  DATETIME DEFAULT GETDATE()
    );
END
ELSE
BEGIN
    PRINT 'Table RHD_2011_Claims already exists.';
END;


/* ============================================================
   STEP 2 — Prevent duplicate execution
   If table already has data, STOP execution.
   ============================================================ */
IF EXISTS (SELECT 1 FROM dbo.RHD_2011_Claims)
BEGIN
    PRINT 'RHD_2011_Claims already populated. Exiting without reprocessing.';
    RETURN;
END;


BEGIN TRY
    BEGIN TRANSACTION;

    /* ============================================================
       STEP 3 — Compute all CTEs (unchanged logic)
       ============================================================ */
    WITH CTE_Calculations AS (
        SELECT 
            pc.hcClaimId,
            SUM(COALESCE(pc.totalPayment, 0)) AS TotalPayment,
            SUM(COALESCE(pc.patientRespAmount, 0)) AS PatientRespAmount
        FROM dbo.Paymentclaim pc
        INNER JOIN dbo.Payment p ON pc.hcPaymentId = p.id
        WHERE p.hcPaymentTypeId IN (1, 2)
          AND pc.dateDeleted IS NULL
        GROUP BY pc.hcClaimId
    ),
    CTE_Charges AS (
        SELECT 
            cce2.hcClaimId,
            SUM(cce2.Charges) AS Charges,
            COUNT(cce2.Id)    AS NoOfCharges,
            MIN(cce2.DateOfService) AS DateOfServiceStart,
            MAX(cce2.DateOfService) AS DateOfServiceEnd
        FROM dbo.ClaimChargeEntries cce2
        WHERE cce2.DateDeleted IS NULL
        GROUP BY cce2.hcClaimId
    ),
    CTE_WriteOff AS (
        SELECT 
            cw.ClaimId,
            SUM(ccew.writeOffAmount) AS WriteOffAmount
        FROM dbo.ClaimWriteOff cw
        INNER JOIN dbo.ClaimChargeEntryWriteOff ccew ON cw.Id = ccew.ClaimWriteOffId
        WHERE cw.dateDeleted IS NULL
          AND ccew.dateDeleted IS NULL
        GROUP BY cw.ClaimId
    ),
    CTE_PositiveAdjustments AS (
        SELECT 
            pc.hcClaimId,
            SUM(pcladj.adjustmentAmount) AS PositiveAdjustment
        FROM dbo.Paymentclaim pc
        INNER JOIN dbo.PaymentClaimServiceLine pcsl ON pc.id = pcsl.hcPaymentClaimId
        INNER JOIN dbo.PaymentClaimServiceLineAdjustment pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
        WHERE pc.dateDeleted IS NULL
          AND pcladj.dateDeleted IS NULL
          AND pcladj.adjustmentGroupCode <> 'PR'
          AND pcladj.IsAdjustmentPositive = 1
        GROUP BY pc.hcClaimId
    ),
    CTE_NegativeAdjustments AS (
        SELECT 
            pc.hcClaimId,
            SUM(pcladj.adjustmentAmount) AS NegativeAdjustment
        FROM dbo.Paymentclaim pc
        INNER JOIN dbo.PaymentClaimServiceLine pcsl ON pc.id = pcsl.hcPaymentClaimId
        INNER JOIN dbo.PaymentClaimServiceLineAdjustment pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
        WHERE pc.dateDeleted IS NULL
          AND pcladj.dateDeleted IS NULL
          AND pcladj.adjustmentGroupCode <> 'PR'
          AND pcladj.IsAdjustmentPositive = 0
        GROUP BY pc.hcClaimId
    ),
    CTE_PositivePatientResponsibility AS (
        SELECT 
            pc.hcClaimId,
            SUM(pcladj.adjustmentAmount) AS PositivePatientResponsibility
        FROM dbo.Paymentclaim pc
        INNER JOIN dbo.PaymentClaimServiceLine pcsl ON pc.id = pcsl.hcPaymentClaimId
        INNER JOIN dbo.PaymentClaimServiceLineAdjustment pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
        WHERE pc.dateDeleted IS NULL
          AND pcladj.dateDeleted IS NULL
          AND pcladj.adjustmentGroupCode = 'PR'
          AND pcladj.IsAdjustmentPositive = 1
        GROUP BY pc.hcClaimId
    ),
    CTE_NegativePatientResponsibility AS (
        SELECT 
            pc.hcClaimId,
            SUM(pcladj.adjustmentAmount) AS NegativePatientResponsibility
        FROM dbo.Paymentclaim pc
        INNER JOIN dbo.PaymentClaimServiceLine pcsl ON pc.id = pcsl.hcPaymentClaimId
        INNER JOIN dbo.PaymentClaimServiceLineAdjustment pcladj ON pcsl.id = pcladj.hcPaymentClaimServiceLineId
        WHERE pc.dateDeleted IS NULL
          AND pcladj.dateDeleted IS NULL
          AND pcladj.adjustmentGroupCode = 'PR'
          AND pcladj.IsAdjustmentPositive = 0
        GROUP BY pc.hcClaimId
    )

    /* ============================================================
       STEP 4 — Insert results into permanent table for backup
       ============================================================ */
    INSERT INTO dbo.RHD_2011_Claims
    (
        Id, AccountInfoId, ClaimNumber, AuthorizationNumber,
        ChildProfileId, ChildProfileAuthorizationId, LocationCodeId,
        DateOfServiceStart, DateOfServiceEnd,
        BilledAmount, ExpectedAmount, BalanceAmount,
        HcClaimStatusId, Status
    )
    SELECT 
        c.Id,
        c.accountInfoId,
        c.ClaimIdentifier,
        COALESCE(c.authorizationNumber, 'N/A'),
        c.childProfileId,
        c.hcChildProfileAuthorizationId,
        c.hcLocationCodeId,
        c.startDate,
        c.endDate,
        cce3.Charges,
        ISNULL(cce3.Charges, 0),
        ISNULL(cce3.Charges, 0)
            - ISNULL(calcNew.TotalPayment, 0)
            + ISNULL(calc.PositiveAdjustment, 0)
            - ISNULL(calc1.NegativeAdjustment, 0)
            - ISNULL(calc2.WriteOffAmount, 0)
            + (ISNULL(calpr1.PositivePatientResponsibility, 0)
                - ISNULL(calpr2.NegativePatientResponsibility, 0)),
        c.hcClaimStatusId,
        cs.Name
    FROM dbo.Claims c
    INNER JOIN dbo.ClaimStatus cs ON c.hcClaimStatusId = cs.id
    LEFT JOIN CTE_WriteOff                      calc2  ON calc2.ClaimId = c.Id
    LEFT JOIN CTE_Calculations                  calcNew ON calcNew.hcClaimId = c.Id
    LEFT JOIN CTE_Charges                       cce3    ON c.Id = cce3.hcClaimId
    LEFT JOIN CTE_PositiveAdjustments           calc    ON calc.hcClaimId = c.Id
    LEFT JOIN CTE_NegativeAdjustments           calc1   ON calc1.hcClaimId = c.Id
    LEFT JOIN CTE_PositivePatientResponsibility calpr1  ON calpr1.hcClaimId = c.Id
    LEFT JOIN CTE_NegativePatientResponsibility calpr2  ON calpr2.hcClaimId = c.Id
    WHERE c.hcClaimStatusId IN (4, 15, 3) AND c.dateDeleted IS NULL
	AND (
            ISNULL(cce3.Charges, 0)
            - ISNULL(calcNew.TotalPayment, 0)
            + ISNULL(calc.PositiveAdjustment, 0)
            - ISNULL(calc1.NegativeAdjustment, 0)
            - ISNULL(calc2.WriteOffAmount, 0)
            + (ISNULL(calpr1.PositivePatientResponsibility, 0)
               - ISNULL(calpr2.NegativePatientResponsibility, 0))
          ) = 0;

    /* ============================================================
       STEP 5 — Only update claims that have Balance = 0
       ============================================================ */
    IF EXISTS (SELECT 1 FROM dbo.RHD_2011_Claims WHERE BalanceAmount = 0)
    BEGIN
        UPDATE c
		SET c.hcClaimStatusId = 6
		FROM dbo.Claims AS c
		INNER JOIN dbo.RHD_2011_Claims AS r ON r.Id = c.Id
		WHERE r.BalanceAmount = 0;

        PRINT 'Claims updated successfully.';
    END
    ELSE
    BEGIN
        PRINT 'No claims eligible for update.';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

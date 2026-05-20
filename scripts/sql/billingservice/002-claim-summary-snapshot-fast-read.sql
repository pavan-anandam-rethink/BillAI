/*
BillingService fast claim summary read path.

This script keeps the GetClaimsByAccountInfoId result shape intact while making
the query optimizer work from dbo.ClaimSummarySnapshot instead of repeatedly
splitting filter strings and evaluating a broad tab/status OR predicate.
*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusClaim' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusClaim
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, ClaimNumber, ClaimId)
    INCLUDE (
        AuthorizationNumber,
        ChildProfileId,
        ChildProfileAuthorizationId,
        LocationCodeId,
        DateOfServiceStart,
        DateOfServiceEnd,
        RenderingProviderId,
        RenderingProviderId2,
        RenderingProviderName,
        BilledAmount,
        ExpectedAmount,
        PaymentAmount,
        BalanceAmount,
        AdjustmentAmount,
        PatientResponsibilityAmount,
        ClaimStatusName,
        hcClaimSubmissionId,
        claimSubmissionIdentifier,
        documentTypeId,
        submissionTypeId,
        frequencyTypeId,
        submissionStatusId,
        submissionStatusName,
        SubmitDate,
        primaryFunderId,
        secondaryFunderId,
        IsSecondaryPayerAvailable,
        FunderId,
        patientName,
        ErrorsCount,
        WarningsCount,
        ResponseCount,
        CMSPagesCount,
        PlaceOfService,
        ChildProfileFunderId,
        FunderName,
        ReasonCodes,
        Reason,
        Comment,
        FlagReasonTransactionId,
        ReasonId,
        HasNote,
        BilledDate,
        AssigneeId,
        hcProviderLocationId,
        FilterTotalPayment,
        FilterPositiveAdjustment,
        FilterNegativeAdjustment,
        FilterWriteOffAmount,
        FilterPositivePatientResp,
        FilterNegativePatientResp,
        HasValidationErrors,
        ValidationSeverityCsv,
        ChargeRenderingProviderIdsCsv
    )
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusDos' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusDos
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, DateOfServiceStart, ClaimId)
    INCLUDE (
        ClaimNumber,
        DateOfServiceEnd,
        BilledAmount,
        BalanceAmount,
        PatientResponsibilityAmount,
        FunderId,
        ChildProfileId,
        AssigneeId,
        hcProviderLocationId,
        ReasonId,
        RenderingProviderId,
        BilledDate
    )
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusPatient' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusPatient
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, ChildProfileId, ClaimId)
    INCLUDE (ClaimNumber, DateOfServiceStart, FunderId, AssigneeId, hcProviderLocationId, ReasonId, RenderingProviderId)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusFunder' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusFunder
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, FunderId, ClaimId)
    INCLUDE (ClaimNumber, DateOfServiceStart, ChildProfileId, AssigneeId, hcProviderLocationId, ReasonId, RenderingProviderId)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusAssignee' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusAssignee
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, AssigneeId, ClaimId)
    INCLUDE (ClaimNumber, DateOfServiceStart, ChildProfileId, FunderId, hcProviderLocationId, ReasonId, RenderingProviderId)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusLocation' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusLocation
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, hcProviderLocationId, ClaimId)
    INCLUDE (ClaimNumber, DateOfServiceStart, ChildProfileId, FunderId, AssigneeId, ReasonId, RenderingProviderId)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusReason' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusReason
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, ReasonId, ClaimId)
    INCLUDE (ClaimNumber, DateOfServiceStart, ChildProfileId, FunderId, AssigneeId, hcProviderLocationId, RenderingProviderId)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusProvider' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_GetClaims_AccountFlagStatusProvider
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, RenderingProviderId, ClaimId)
    INCLUDE (ClaimNumber, DateOfServiceStart, ChildProfileId, FunderId, AssigneeId, hcProviderLocationId, ReasonId)
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.GetClaimsByAccountInfoId
(
    @AccountInfoId                  INT,
    @Skip                           INT,
    @Take                           INT,
    @OrderField                     NVARCHAR(40) = '',
    @OrderDir                       BIT = NULL,
    @ClaimNumber                    NVARCHAR(20) = NULL,
    @ClaimIds                       NVARCHAR(200) = NULL,
    @PatientIds                     NVARCHAR(100) = NULL,
    @FunderIds                      NVARCHAR(100) = NULL,
    @AssigneeIds                    NVARCHAR(200) = NULL,
    @LocationIds                    NVARCHAR(200) = NULL,
    @ReasonIds                      NVARCHAR(200) = NULL,
    @BalanceFrom                    DECIMAL(18, 2) = NULL,
    @BalanceTo                      DECIMAL(18, 2) = NULL,
    @BilledFrom                     DECIMAL(18, 2) = NULL,
    @BilledTo                       DECIMAL(18, 2) = NULL,
    @PatientResponsibilityFrom      INT = NULL,
    @PatientResponsibilityTo        INT = NULL,
    @DateOfServiceFrom              DATETIME = NULL,
    @DateOfServiceTo                DATETIME = NULL,
    @RenderingProviderIds           NVARCHAR(100) = NULL,
    @StatusIds                      NVARCHAR(100) = NULL,
    @Tab                            INT,
    @ShowVoided                     BIT,
    @ValidationIds                  NVARCHAR(100) = NULL,
    @ResponseIds                    NVARCHAR(100) = NULL,
    @ReasonCode                     NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ShowWithNoErrorsIndicator INT = 99;
    DECLARE @TabIsFlagged BIT = CASE WHEN @Tab = 7 THEN 1 ELSE 0 END;
    DECLARE @FetchRows INT = CASE WHEN @Take = 0 THEN 2147483647 ELSE @Take END;

    CREATE TABLE #TabStatusFilter (Status INT NOT NULL PRIMARY KEY);

    IF @Tab = 1
        INSERT INTO #TabStatusFilter (Status) VALUES (1), (20);
    ELSE IF @Tab = 2
        INSERT INTO #TabStatusFilter (Status) VALUES (2), (7), (10), (14), (19);
    ELSE IF @Tab = 3
        INSERT INTO #TabStatusFilter (Status) VALUES (3), (4), (11), (12), (15), (16), (17);
    ELSE IF @Tab = 4
    BEGIN
        INSERT INTO #TabStatusFilter (Status) VALUES (6), (13);

        IF @ShowVoided = 1
            INSERT INTO #TabStatusFilter (Status) VALUES (18);
    END;
    ELSE IF @Tab = 5
        INSERT INTO #TabStatusFilter (Status) VALUES (8), (9);
    ELSE IF @Tab = 6
        INSERT INTO #TabStatusFilter (Status) VALUES (5);

    CREATE TABLE #ClaimIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @ClaimIds IS NOT NULL AND @ClaimIds <> ''
        INSERT INTO #ClaimIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ClaimIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #PatientIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @PatientIds IS NOT NULL AND @PatientIds <> ''
        INSERT INTO #PatientIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@PatientIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #FunderIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @FunderIds IS NOT NULL AND @FunderIds <> ''
        INSERT INTO #FunderIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@FunderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #AssigneeIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @AssigneeIds IS NOT NULL AND @AssigneeIds <> ''
        INSERT INTO #AssigneeIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@AssigneeIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #LocationIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @LocationIds IS NOT NULL AND @LocationIds <> ''
        INSERT INTO #LocationIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@LocationIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #ReasonIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @ReasonIds IS NOT NULL AND @ReasonIds <> ''
        INSERT INTO #ReasonIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ReasonIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #RenderingProviderIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> ''
        INSERT INTO #RenderingProviderIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@RenderingProviderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #StatusIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @StatusIds IS NOT NULL AND @StatusIds <> ''
        INSERT INTO #StatusIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@StatusIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    CREATE TABLE #ValidationIdFilter (Severity INT NOT NULL PRIMARY KEY);
    IF @ValidationIds IS NOT NULL AND @ValidationIds <> ''
        INSERT INTO #ValidationIdFilter (Severity)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ValidationIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #ResponseIdFilter (Value NVARCHAR(100) NOT NULL PRIMARY KEY);
    IF @ResponseIds IS NOT NULL AND @ResponseIds <> ''
        INSERT INTO #ResponseIdFilter (Value)
        SELECT DISTINCT LTRIM(RTRIM(value))
        FROM STRING_SPLIT(@ResponseIds, ',');

    DECLARE @HasClaimIdFilter BIT = CASE WHEN @ClaimIds IS NOT NULL AND @ClaimIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasPatientIdFilter BIT = CASE WHEN @PatientIds IS NOT NULL AND @PatientIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasFunderIdFilter BIT = CASE WHEN @FunderIds IS NOT NULL AND @FunderIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasAssigneeIdFilter BIT = CASE WHEN @AssigneeIds IS NOT NULL AND @AssigneeIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasLocationIdFilter BIT = CASE WHEN @LocationIds IS NOT NULL AND @LocationIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasReasonIdFilter BIT = CASE WHEN @ReasonIds IS NOT NULL AND @ReasonIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasRenderingProviderIdFilter BIT = CASE WHEN @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasStatusIdFilter BIT = CASE WHEN @StatusIds IS NOT NULL AND @StatusIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasValidationIdFilter BIT = CASE WHEN @ValidationIds IS NOT NULL AND @ValidationIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasResponseIdFilter BIT = CASE WHEN @ResponseIds IS NOT NULL AND @ResponseIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ShowNoValidationErrors BIT =
        CASE
            WHEN @ValidationIds IS NOT NULL
             AND CHARINDEX(CAST(@ShowWithNoErrorsIndicator AS VARCHAR(10)), @ValidationIds) > 0 THEN 1
            ELSE 0
        END;
    DECLARE @ResponseIdsLegacyMatch BIT = 0;

    IF @HasResponseIdFilter = 1
       AND EXISTS (
            SELECT 1
            FROM #ResponseIdFilter
            WHERE Value = CAST(LTRIM(RTRIM(@ResponseIds)) AS VARCHAR(100))
       )
    BEGIN
        SET @ResponseIdsLegacyMatch = 1;
    END;

    SELECT
        s.ClaimId AS Id,
        s.ClaimNumber,
        s.AuthorizationNumber,
        s.ChildProfileId,
        s.ChildProfileAuthorizationId,
        s.LocationCodeId,
        s.DateOfServiceStart,
        s.DateOfServiceEnd,
        s.RenderingProviderId,
        s.RenderingProviderId2,
        s.RenderingProviderName,
        s.BilledAmount,
        s.ExpectedAmount,
        s.PaymentAmount,
        s.BalanceAmount,
        s.AdjustmentAmount,
        s.PatientResponsibilityAmount,
        s.Status,
        s.ClaimStatusName,
        s.hcClaimSubmissionId,
        s.claimSubmissionIdentifier,
        s.documentTypeId,
        s.submissionTypeId,
        s.frequencyTypeId,
        s.submissionStatusId,
        s.submissionStatusName,
        s.SubmitDate,
        s.primaryFunderId,
        s.secondaryFunderId,
        s.IsSecondaryPayerAvailable,
        s.FunderId,
        s.patientName,
        s.ErrorsCount,
        s.WarningsCount,
        s.ResponseCount,
        s.CMSPagesCount,
        s.PlaceOfService,
        s.ChildProfileFunderId,
        s.FunderName,
        s.ReasonCodes,
        s.Reason,
        s.Comment,
        s.FlagReasonTransactionId,
        s.ReasonId,
        s.HasNote,
        s.BilledDate,
        s.AssigneeId,
        COUNT(*) OVER () AS totalCount
    FROM dbo.ClaimSummarySnapshot s
    CROSS APPLY (
        SELECT
            BalanceFilterAmount =
                ISNULL(s.BilledAmount, 0) - ISNULL(s.FilterTotalPayment, 0)
                + ISNULL(s.FilterPositiveAdjustment, 0) - ISNULL(s.FilterNegativeAdjustment, 0)
                - ISNULL(s.FilterWriteOffAmount, 0)
                + ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0),
            PatientResponsibilityFilterAmount =
                ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)
    ) f
    WHERE s.AccountInfoId = @AccountInfoId
      AND s.IsFlagged = @TabIsFlagged
      AND (@Tab = 7 OR s.Status IN (SELECT Status FROM #TabStatusFilter))
      AND (@ClaimNumber IS NULL OR s.ClaimNumber LIKE @ClaimNumber + '%')
      AND (@HasPatientIdFilter = 0 OR s.ChildProfileId IN (SELECT Id FROM #PatientIdFilter))
      AND (@HasClaimIdFilter = 0 OR s.ClaimId IN (SELECT Id FROM #ClaimIdFilter))
      AND (@HasReasonIdFilter = 0 OR s.ReasonId IN (SELECT Id FROM #ReasonIdFilter))
      AND (@HasFunderIdFilter = 0 OR s.FunderId IN (SELECT Id FROM #FunderIdFilter))
      AND (@HasAssigneeIdFilter = 0 OR s.AssigneeId IN (SELECT Id FROM #AssigneeIdFilter))
      AND (@HasLocationIdFilter = 0 OR s.hcProviderLocationId IN (SELECT Id FROM #LocationIdFilter))
      AND (
            @HasRenderingProviderIdFilter = 0
            OR s.RenderingProviderId IN (SELECT Id FROM #RenderingProviderIdFilter)
            OR (
                s.ChargeRenderingProviderIdsCsv IS NOT NULL
                AND EXISTS (
                    SELECT 1
                    FROM #RenderingProviderIdFilter rf
                    WHERE CHARINDEX(',' + CAST(rf.Id AS VARCHAR(20)) + ',', ',' + s.ChargeRenderingProviderIdsCsv + ',') > 0
                )
            )
          )
      AND (@BalanceFrom IS NULL OR @BalanceFrom <= f.BalanceFilterAmount)
      AND (@BalanceTo IS NULL OR @BalanceTo >= f.BalanceFilterAmount)
      AND (@PatientResponsibilityFrom IS NULL OR @PatientResponsibilityFrom <= f.PatientResponsibilityFilterAmount)
      AND (@PatientResponsibilityTo IS NULL OR @PatientResponsibilityTo >= f.PatientResponsibilityFilterAmount)
      AND (@DateOfServiceFrom IS NULL OR s.DateOfServiceStart >= @DateOfServiceFrom)
      AND (@DateOfServiceTo IS NULL OR s.DateOfServiceStart <= @DateOfServiceTo)
      AND (@BilledFrom IS NULL OR @BilledFrom <= s.BilledAmount)
      AND (@BilledTo IS NULL OR @BilledTo >= s.BilledAmount)
      AND (@HasStatusIdFilter = 0 OR s.Status IN (SELECT Id FROM #StatusIdFilter))
      AND (
            @HasValidationIdFilter = 0
            OR EXISTS (
                SELECT 1
                FROM #ValidationIdFilter vf
                WHERE vf.Severity <> @ShowWithNoErrorsIndicator
                  AND s.ValidationSeverityCsv IS NOT NULL
                  AND CHARINDEX(',' + CAST(vf.Severity AS VARCHAR(10)) + ',', ',' + s.ValidationSeverityCsv + ',') > 0
            )
            OR (@ShowNoValidationErrors = 1 AND s.HasValidationErrors = 0)
          )
      AND (
            @HasResponseIdFilter = 0
            OR (@ResponseIdsLegacyMatch = 1 AND s.ResponseCount > 0)
          )
    ORDER BY
        CASE WHEN @OrderField = 'claimNumber' AND @OrderDir = 0 THEN s.ClaimNumber END ASC,
        CASE WHEN @OrderField = 'claimNumber' AND @OrderDir = 1 THEN s.ClaimNumber END DESC,
        CASE WHEN @OrderField = 'DateOfServiceStart' AND @OrderDir = 0 THEN s.DateOfServiceStart END ASC,
        CASE WHEN @OrderField = 'DateOfServiceStart' AND @OrderDir = 1 THEN s.DateOfServiceStart END DESC,
        CASE WHEN @OrderField = 'DateOfServiceEnd' AND @OrderDir = 0 THEN s.DateOfServiceEnd END ASC,
        CASE WHEN @OrderField = 'DateOfServiceEnd' AND @OrderDir = 1 THEN s.DateOfServiceEnd END DESC,
        CASE WHEN @OrderField = 'BilledAmount' AND @OrderDir = 0 THEN s.BilledAmount END ASC,
        CASE WHEN @OrderField = 'BilledAmount' AND @OrderDir = 1 THEN s.BilledAmount END DESC,
        CASE WHEN @OrderField = 'PatientResponsibilityAmount' AND @OrderDir = 0 THEN s.PatientResponsibilityAmount END ASC,
        CASE WHEN @OrderField = 'PatientResponsibilityAmount' AND @OrderDir = 1 THEN s.PatientResponsibilityAmount END DESC,
        CASE WHEN @OrderField = 'ExpectedAmount' AND @OrderDir = 0 THEN s.ExpectedAmount END ASC,
        CASE WHEN @OrderField = 'ExpectedAmount' AND @OrderDir = 1 THEN s.ExpectedAmount END DESC,
        CASE WHEN @OrderField = 'PaymentAmount' AND @OrderDir = 0 THEN s.PaymentAmount END ASC,
        CASE WHEN @OrderField = 'PaymentAmount' AND @OrderDir = 1 THEN s.PaymentAmount END DESC,
        CASE WHEN @OrderField = 'BalanceAmount' AND @OrderDir = 0 THEN s.BalanceAmount END ASC,
        CASE WHEN @OrderField = 'BalanceAmount' AND @OrderDir = 1 THEN s.BalanceAmount END DESC,
        CASE WHEN @OrderField = 'Status' AND @OrderDir = 0 THEN s.Status END ASC,
        CASE WHEN @OrderField = 'Status' AND @OrderDir = 1 THEN s.Status END DESC,
        CASE WHEN @OrderField = 'PatientName' AND @OrderDir = 0 THEN s.patientName END ASC,
        CASE WHEN @OrderField = 'PatientName' AND @OrderDir = 1 THEN s.patientName END DESC,
        CASE WHEN @OrderField = 'FunderName' AND @OrderDir = 0 THEN s.FunderName END ASC,
        CASE WHEN @OrderField = 'FunderName' AND @OrderDir = 1 THEN s.FunderName END DESC,
        CASE WHEN @OrderField = 'AuthorizationNumber' AND @OrderDir = 0 THEN s.AuthorizationNumber END ASC,
        CASE WHEN @OrderField = 'AuthorizationNumber' AND @OrderDir = 1 THEN s.AuthorizationNumber END DESC,
        CASE WHEN @OrderField = 'PlaceOfService' AND @OrderDir = 0 THEN s.PlaceOfService END ASC,
        CASE WHEN @OrderField = 'PlaceOfService' AND @OrderDir = 1 THEN s.PlaceOfService END DESC,
        CASE WHEN @OrderField = 'RenderingProviderName' AND @OrderDir = 0 THEN s.RenderingProviderName END ASC,
        CASE WHEN @OrderField = 'RenderingProviderName' AND @OrderDir = 1 THEN s.RenderingProviderName END DESC,
        CASE WHEN @OrderField = 'BilledDate' AND @OrderDir = 0 THEN s.BilledDate END ASC,
        CASE WHEN @OrderField = 'BilledDate' AND @OrderDir = 1 THEN s.BilledDate END DESC
    OFFSET @Skip ROWS
    FETCH NEXT @FetchRows ROWS ONLY
    OPTION (RECOMPILE);
END;
GO

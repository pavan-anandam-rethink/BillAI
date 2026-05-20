/*
GetClaimsByAccountInfoId tuning for ClaimSummarySnapshot-backed reads.

- Narrows paging to ClaimId/row number before the wide projection.
- Uses temp tables so SQL Server has statistics for list filters.
- Keeps the procedure bound to dbo.ClaimSummarySnapshot only.
- Adds filtered browse indexes for current and flagged claims.

Review the actual execution plan in the target Azure SQL database before rollout.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_CurrentBrowse'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_CurrentBrowse
    ON dbo.ClaimSummarySnapshot (AccountInfoId, Status, ClaimId)
    INCLUDE (
        ClaimNumber,
        ChildProfileId,
        ReasonId,
        FunderId,
        AssigneeId,
        hcProviderLocationId,
        RenderingProviderId,
        DateOfServiceStart,
        DateOfServiceEnd,
        BilledAmount,
        ExpectedAmount,
        PaymentAmount,
        BalanceAmount,
        PatientResponsibilityAmount,
        patientName,
        FunderName,
        AuthorizationNumber,
        PlaceOfService,
        RenderingProviderName,
        BilledDate,
        ResponseCount,
        HasValidationErrors,
        FilterTotalPayment,
        FilterPositiveAdjustment,
        FilterNegativeAdjustment,
        FilterWriteOffAmount,
        FilterPositivePatientResp,
        FilterNegativePatientResp
    )
    WHERE IsFlagged = 0
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_FlaggedBrowse'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_FlaggedBrowse
    ON dbo.ClaimSummarySnapshot (AccountInfoId, ClaimId)
    INCLUDE (
        Status,
        ClaimNumber,
        ChildProfileId,
        ReasonId,
        FunderId,
        AssigneeId,
        hcProviderLocationId,
        RenderingProviderId,
        DateOfServiceStart,
        DateOfServiceEnd,
        BilledAmount,
        ExpectedAmount,
        PaymentAmount,
        BalanceAmount,
        PatientResponsibilityAmount,
        patientName,
        FunderName,
        AuthorizationNumber,
        PlaceOfService,
        RenderingProviderName,
        BilledDate,
        ResponseCount,
        HasValidationErrors,
        FilterTotalPayment,
        FilterPositiveAdjustment,
        FilterNegativeAdjustment,
        FilterWriteOffAmount,
        FilterPositivePatientResp,
        FilterNegativePatientResp
    )
    WHERE IsFlagged = 1
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
    @PatientResponsibilityFrom      DECIMAL(18, 2) = NULL,
    @PatientResponsibilityTo        DECIMAL(18, 2) = NULL,
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
    DECLARE @EffectiveTake INT = CASE WHEN ISNULL(@Take, 0) = 0 THEN 2147483647 ELSE @Take END;
    DECLARE @SkipRow BIGINT = CAST(ISNULL(@Skip, 0) AS BIGINT);
    DECLARE @LastRow BIGINT = @SkipRow + CAST(@EffectiveTake AS BIGINT);
    /*
    Snapshot only exposes response presence through ResponseCount, so the lightweight proc
    treats any non-empty response filter as "claim has response data" without rejoining
    response detail rows.
    */
    DECLARE @HasResponseFilter BIT =
        CASE
            WHEN @ResponseIds IS NULL OR CAST(@ResponseIds AS VARCHAR(100)) = '' THEN 0
            ELSE 1
        END;
    DECLARE @IncludeClaimsWithoutValidationErrors BIT = 0;
    DECLARE @HasClaimIdFilter BIT = CASE WHEN @ClaimIds IS NOT NULL AND @ClaimIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasPatientIdFilter BIT = CASE WHEN @PatientIds IS NOT NULL AND @PatientIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasFunderIdFilter BIT = CASE WHEN @FunderIds IS NOT NULL AND @FunderIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasAssigneeIdFilter BIT = CASE WHEN @AssigneeIds IS NOT NULL AND @AssigneeIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasLocationIdFilter BIT = CASE WHEN @LocationIds IS NOT NULL AND @LocationIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasReasonIdFilter BIT = CASE WHEN @ReasonIds IS NOT NULL AND @ReasonIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasRenderingProviderFilter BIT = CASE WHEN @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasStatusFilter BIT = CASE WHEN @StatusIds IS NOT NULL AND @StatusIds <> '' THEN 1 ELSE 0 END;
    DECLARE @HasValidationFilter BIT = CASE WHEN @ValidationIds IS NOT NULL AND @ValidationIds <> '' THEN 1 ELSE 0 END;
    DECLARE @OrderExpression NVARCHAR(128);
    DECLARE @OrderClause NVARCHAR(160);
    DECLARE @OrderDirection NVARCHAR(5) = CASE WHEN @OrderDir = 1 THEN N'DESC' ELSE N'ASC' END;
    DECLARE @Sql NVARCHAR(MAX);

    CREATE TABLE #ClaimIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #PatientIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #FunderIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #AssigneeIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #LocationIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #ReasonIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #RenderingProviderIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #StatusIdFilter (Id INT NOT NULL PRIMARY KEY);
    CREATE TABLE #ValidationIdFilter (Severity INT NOT NULL PRIMARY KEY);

    IF @ClaimIds IS NOT NULL AND @ClaimIds <> ''
    BEGIN
        INSERT INTO #ClaimIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ClaimIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @PatientIds IS NOT NULL AND @PatientIds <> ''
    BEGIN
        INSERT INTO #PatientIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@PatientIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @FunderIds IS NOT NULL AND @FunderIds <> ''
    BEGIN
        INSERT INTO #FunderIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@FunderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @AssigneeIds IS NOT NULL AND @AssigneeIds <> ''
    BEGIN
        INSERT INTO #AssigneeIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@AssigneeIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @LocationIds IS NOT NULL AND @LocationIds <> ''
    BEGIN
        INSERT INTO #LocationIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@LocationIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @ReasonIds IS NOT NULL AND @ReasonIds <> ''
    BEGIN
        INSERT INTO #ReasonIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ReasonIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> ''
    BEGIN
        INSERT INTO #RenderingProviderIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@RenderingProviderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @StatusIds IS NOT NULL AND @StatusIds <> ''
    BEGIN
        INSERT INTO #StatusIdFilter (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@StatusIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;
    END;

    IF @ValidationIds IS NOT NULL AND @ValidationIds <> ''
    BEGIN
        INSERT INTO #ValidationIdFilter (Severity)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ValidationIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    END;

    SET @IncludeClaimsWithoutValidationErrors =
        CASE WHEN EXISTS (
            SELECT 1
            FROM #ValidationIdFilter
            WHERE Severity = @ShowWithNoErrorsIndicator
        )
        THEN 1
        ELSE 0
        END;

    CREATE TABLE #PageClaims
    (
        RowNum BIGINT NOT NULL PRIMARY KEY,
        ClaimId INT NOT NULL UNIQUE,
        TotalCount INT NOT NULL
    );

    SET @OrderExpression =
        CASE @OrderField
            WHEN N'claimNumber' THEN N's.ClaimNumber'
            WHEN N'DateOfServiceStart' THEN N's.DateOfServiceStart'
            WHEN N'DateOfServiceEnd' THEN N's.DateOfServiceEnd'
            WHEN N'BilledAmount' THEN N's.BilledAmount'
            WHEN N'PatientResponsibilityAmount' THEN N's.PatientResponsibilityAmount'
            WHEN N'ExpectedAmount' THEN N's.ExpectedAmount'
            WHEN N'PaymentAmount' THEN N's.PaymentAmount'
            WHEN N'BalanceAmount' THEN N's.BalanceAmount'
            WHEN N'Status' THEN N's.Status'
            WHEN N'PatientName' THEN N's.patientName'
            WHEN N'FunderName' THEN N's.FunderName'
            WHEN N'AuthorizationNumber' THEN N's.AuthorizationNumber'
            WHEN N'PlaceOfService' THEN N's.PlaceOfService'
            WHEN N'RenderingProviderName' THEN N's.RenderingProviderName'
            WHEN N'BilledDate' THEN N's.BilledDate'
            ELSE NULL
        END;

    SET @OrderClause =
        CASE
            WHEN @OrderDir IS NULL OR @OrderExpression IS NULL THEN N'(SELECT NULL)'
            ELSE @OrderExpression + N' ' + @OrderDirection
        END;

    SET @Sql = N'
;WITH FilteredClaims AS
(
    SELECT
        s.ClaimId,
        ROW_NUMBER() OVER (ORDER BY ' + @OrderClause + N') AS RowNum,
        COUNT_BIG(1) OVER () AS TotalCount
    FROM dbo.ClaimSummarySnapshot AS s
    WHERE s.AccountInfoId = @AccountInfoId
      AND (
            (@Tab = 1 AND s.IsFlagged = 0 AND s.Status IN (1, 20))
            OR (@Tab = 2 AND s.IsFlagged = 0 AND s.Status IN (2, 7, 10, 14, 19))
            OR (@Tab = 3 AND s.IsFlagged = 0 AND s.Status IN (3, 4, 11, 12, 15, 16, 17))
            OR (@Tab = 4 AND s.IsFlagged = 0 AND (((@ShowVoided = 1) AND s.Status = 18) OR s.Status IN (6, 13)))
            OR (@Tab = 5 AND s.IsFlagged = 0 AND s.Status IN (8, 9))
            OR (@Tab = 6 AND s.IsFlagged = 0 AND s.Status = 5)
            OR (@Tab = 7 AND s.IsFlagged = 1)
          )
      AND (@ClaimNumber IS NULL OR s.ClaimNumber LIKE @ClaimNumber + ''%'')
      AND (@HasPatientIdFilter = 0 OR s.ChildProfileId IN (SELECT Id FROM #PatientIdFilter))
      AND (@HasClaimIdFilter = 0 OR s.ClaimId IN (SELECT Id FROM #ClaimIdFilter))
      AND (@HasReasonIdFilter = 0 OR s.ReasonId IN (SELECT Id FROM #ReasonIdFilter))
      AND (@HasFunderIdFilter = 0 OR s.FunderId IN (SELECT Id FROM #FunderIdFilter))
      AND (@HasAssigneeIdFilter = 0 OR s.AssigneeId IN (SELECT Id FROM #AssigneeIdFilter))
      AND (@HasLocationIdFilter = 0 OR s.hcProviderLocationId IN (SELECT Id FROM #LocationIdFilter))
      AND (
            @HasRenderingProviderFilter = 0
            OR s.RenderingProviderId IN (SELECT Id FROM #RenderingProviderIdFilter)
            OR (
                s.ChargeRenderingProviderIdsCsv IS NOT NULL
                AND EXISTS (
                    SELECT 1
                    FROM #RenderingProviderIdFilter AS rf
                    WHERE CHARINDEX('','' + CONVERT(VARCHAR(20), rf.Id) + '','', '','' + s.ChargeRenderingProviderIdsCsv + '','') > 0
                )
            )
          )
      AND (
            @BalanceFrom IS NULL
            OR @BalanceFrom <= ISNULL(s.BilledAmount, 0) - ISNULL(s.FilterTotalPayment, 0)
                + ISNULL(s.FilterPositiveAdjustment, 0) - ISNULL(s.FilterNegativeAdjustment, 0)
                - ISNULL(s.FilterWriteOffAmount, 0)
                + ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)
          )
      AND (
            @BalanceTo IS NULL
            OR @BalanceTo >= ISNULL(s.BilledAmount, 0) - ISNULL(s.FilterTotalPayment, 0)
                + ISNULL(s.FilterPositiveAdjustment, 0) - ISNULL(s.FilterNegativeAdjustment, 0)
                - ISNULL(s.FilterWriteOffAmount, 0)
                + ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)
          )
      AND (
            @PatientResponsibilityFrom IS NULL
            OR @PatientResponsibilityFrom <= ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)
          )
      AND (
            @PatientResponsibilityTo IS NULL
            OR @PatientResponsibilityTo >= ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)
          )
      AND (@DateOfServiceFrom IS NULL OR s.DateOfServiceStart >= @DateOfServiceFrom)
      AND (@DateOfServiceTo IS NULL OR s.DateOfServiceStart <= @DateOfServiceTo)
      AND (@BilledFrom IS NULL OR @BilledFrom <= s.BilledAmount)
      AND (@BilledTo IS NULL OR @BilledTo >= s.BilledAmount)
      AND (@HasStatusFilter = 0 OR s.Status IN (SELECT Id FROM #StatusIdFilter))
      AND (
            @HasValidationFilter = 0
            OR EXISTS (
                SELECT 1
                FROM #ValidationIdFilter AS vf
                WHERE vf.Severity <> @ShowWithNoErrorsIndicator
                  AND s.ValidationSeverityCsv IS NOT NULL
                  AND CHARINDEX('','' + CONVERT(VARCHAR(10), vf.Severity) + '','', '','' + s.ValidationSeverityCsv + '','') > 0
            )
            OR (@IncludeClaimsWithoutValidationErrors = 1 AND s.HasValidationErrors = 0)
          )
      AND (
            @HasResponseFilter = 0
            OR ISNULL(s.ResponseCount, 0) > 0
          )
)
INSERT INTO #PageClaims (RowNum, ClaimId, TotalCount)
SELECT
    fc.RowNum,
    fc.ClaimId,
    CONVERT(INT, fc.TotalCount)
FROM FilteredClaims AS fc
WHERE fc.RowNum > @SkipRow
  AND fc.RowNum <= @LastRow
OPTION (RECOMPILE);';

    EXEC sp_executesql
        @Sql,
        N'@AccountInfoId INT,
          @Tab INT,
          @ShowVoided BIT,
          @ClaimNumber NVARCHAR(20),
          @BalanceFrom DECIMAL(18, 2),
          @BalanceTo DECIMAL(18, 2),
          @PatientResponsibilityFrom DECIMAL(18, 2),
          @PatientResponsibilityTo DECIMAL(18, 2),
          @DateOfServiceFrom DATETIME,
          @DateOfServiceTo DATETIME,
          @BilledFrom DECIMAL(18, 2),
          @BilledTo DECIMAL(18, 2),
          @HasClaimIdFilter BIT,
          @HasPatientIdFilter BIT,
          @HasFunderIdFilter BIT,
          @HasAssigneeIdFilter BIT,
          @HasLocationIdFilter BIT,
          @HasReasonIdFilter BIT,
          @HasRenderingProviderFilter BIT,
          @HasStatusFilter BIT,
          @HasValidationFilter BIT,
          @ShowWithNoErrorsIndicator INT,
          @IncludeClaimsWithoutValidationErrors BIT,
          @HasResponseFilter BIT,
          @SkipRow BIGINT,
          @LastRow BIGINT',
        @AccountInfoId = @AccountInfoId,
        @Tab = @Tab,
        @ShowVoided = @ShowVoided,
        @ClaimNumber = @ClaimNumber,
        @BalanceFrom = @BalanceFrom,
        @BalanceTo = @BalanceTo,
        @PatientResponsibilityFrom = @PatientResponsibilityFrom,
        @PatientResponsibilityTo = @PatientResponsibilityTo,
        @DateOfServiceFrom = @DateOfServiceFrom,
        @DateOfServiceTo = @DateOfServiceTo,
        @BilledFrom = @BilledFrom,
        @BilledTo = @BilledTo,
        @HasClaimIdFilter = @HasClaimIdFilter,
        @HasPatientIdFilter = @HasPatientIdFilter,
        @HasFunderIdFilter = @HasFunderIdFilter,
        @HasAssigneeIdFilter = @HasAssigneeIdFilter,
        @HasLocationIdFilter = @HasLocationIdFilter,
        @HasReasonIdFilter = @HasReasonIdFilter,
        @HasRenderingProviderFilter = @HasRenderingProviderFilter,
        @HasStatusFilter = @HasStatusFilter,
        @HasValidationFilter = @HasValidationFilter,
        @ShowWithNoErrorsIndicator = @ShowWithNoErrorsIndicator,
        @IncludeClaimsWithoutValidationErrors = @IncludeClaimsWithoutValidationErrors,
        @HasResponseFilter = @HasResponseFilter,
        @SkipRow = @SkipRow,
        @LastRow = @LastRow;

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
        pc.TotalCount AS totalCount
    FROM #PageClaims AS pc
    INNER JOIN dbo.ClaimSummarySnapshot AS s
        ON s.ClaimId = pc.ClaimId
    ORDER BY pc.RowNum;
END
GO

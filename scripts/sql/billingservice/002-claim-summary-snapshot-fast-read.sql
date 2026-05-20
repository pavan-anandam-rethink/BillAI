/*
BillingService GetClaimsByAccountInfoId snapshot fast-read optimization.

This script is intentionally additive for indexes and idempotent for the procedure.
It keeps the result columns used by BillingService while avoiding wide-row sorting
and optional-filter OR predicates against dbo.ClaimSummarySnapshot.
*/

IF OBJECT_ID(N'dbo.ClaimSummarySnapshot', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_Status_ClaimId' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Flag_Status_ClaimId
        ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, ClaimId)
        INCLUDE (
            ClaimNumber,
            ChildProfileId,
            FunderId,
            AssigneeId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceStart,
            DateOfServiceEnd,
            BilledAmount,
            BilledDate,
            PatientResponsibilityAmount,
            BalanceAmount,
            FilterTotalPayment,
            FilterPositiveAdjustment,
            FilterNegativeAdjustment,
            FilterWriteOffAmount,
            FilterPositivePatientResp,
            FilterNegativePatientResp,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_ClaimNumber' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Flag_ClaimNumber
        ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, ClaimNumber, Status, ClaimId)
        INCLUDE (
            ChildProfileId,
            FunderId,
            AssigneeId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceStart,
            BilledAmount,
            BilledDate,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_DosStart' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Flag_DosStart
        ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, DateOfServiceStart, Status, ClaimId)
        INCLUDE (
            ClaimNumber,
            ChildProfileId,
            FunderId,
            AssigneeId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceEnd,
            BilledAmount,
            BilledDate,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Flag_BilledDate' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Flag_BilledDate
        ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, BilledDate, Status, ClaimId)
        INCLUDE (
            ClaimNumber,
            ChildProfileId,
            FunderId,
            AssigneeId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceStart,
            BilledAmount,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Patient_Filter' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Patient_Filter
        ON dbo.ClaimSummarySnapshot (AccountInfoId, ChildProfileId, IsFlagged, Status, ClaimId)
        INCLUDE (
            ClaimNumber,
            FunderId,
            AssigneeId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceStart,
            BilledAmount,
            BilledDate,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Funder_Filter' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Funder_Filter
        ON dbo.ClaimSummarySnapshot (AccountInfoId, FunderId, IsFlagged, Status, ClaimId)
        INCLUDE (
            ClaimNumber,
            ChildProfileId,
            AssigneeId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceStart,
            BilledAmount,
            BilledDate,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClaimSummarySnapshot_Account_Assignee_Filter' AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_Assignee_Filter
        ON dbo.ClaimSummarySnapshot (AccountInfoId, AssigneeId, IsFlagged, Status, ClaimId)
        INCLUDE (
            ClaimNumber,
            ChildProfileId,
            FunderId,
            hcProviderLocationId,
            ReasonId,
            RenderingProviderId,
            DateOfServiceStart,
            BilledAmount,
            BilledDate,
            HasValidationErrors
        )
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;
END;
GO

IF OBJECT_ID(N'dbo.ClearingHouseResponseDetails', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClearingHouseResponseDetails_Claim_ResponseType' AND object_id = OBJECT_ID(N'dbo.ClearingHouseResponseDetails'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ClearingHouseResponseDetails_Claim_ResponseType
        ON dbo.ClearingHouseResponseDetails (ClaimId, ResponseFileTypeId)
        INCLUDE (DateDeleted)
        WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
    END;
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
    DECLARE @TakeRows INT = CASE WHEN ISNULL(@Take, 0) = 0 THEN 2147483647 ELSE @Take END;
    DECLARE @TotalCount INT = 0;
    DECLARE @HasNoErrorValidation BIT = 0;

    IF @Skip IS NULL OR @Skip < 0
        SET @Skip = 0;

    IF @TakeRows < 0
        SET @TakeRows = 2147483647;

    CREATE TABLE #ClaimIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @ClaimIds IS NOT NULL AND @ClaimIds <> ''
        INSERT INTO #ClaimIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ClaimIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #PatientIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @PatientIds IS NOT NULL AND @PatientIds <> ''
        INSERT INTO #PatientIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@PatientIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #FunderIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @FunderIds IS NOT NULL AND @FunderIds <> ''
        INSERT INTO #FunderIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@FunderIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #AssigneeIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @AssigneeIds IS NOT NULL AND @AssigneeIds <> ''
        INSERT INTO #AssigneeIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@AssigneeIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #LocationIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @LocationIds IS NOT NULL AND @LocationIds <> ''
        INSERT INTO #LocationIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@LocationIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #ReasonIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @ReasonIds IS NOT NULL AND @ReasonIds <> ''
        INSERT INTO #ReasonIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ReasonIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #RenderingProviderIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> ''
        INSERT INTO #RenderingProviderIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@RenderingProviderIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #StatusIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @StatusIds IS NOT NULL AND @StatusIds <> ''
        INSERT INTO #StatusIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@StatusIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #ValidationIdFilter (Severity INT NOT NULL PRIMARY KEY);
    IF @ValidationIds IS NOT NULL AND @ValidationIds <> ''
    BEGIN
        INSERT INTO #ValidationIdFilter (Severity)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ValidationIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

        IF EXISTS (SELECT 1 FROM #ValidationIdFilter WHERE Severity = @ShowWithNoErrorsIndicator)
            SET @HasNoErrorValidation = 1;
    END;

    CREATE TABLE #ResponseIdFilter (Id INT NOT NULL PRIMARY KEY);
    IF @ResponseIds IS NOT NULL AND @ResponseIds <> ''
        INSERT INTO #ResponseIdFilter (Id)
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ResponseIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    CREATE TABLE #PageClaimIds
    (
        SortOrdinal INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        ClaimId INT NOT NULL UNIQUE
    );

    DECLARE @FromWhereSql NVARCHAR(MAX) = N'
FROM dbo.ClaimSummarySnapshot s
WHERE s.AccountInfoId = @AccountInfoId';

    SET @FromWhereSql += CASE @Tab
        WHEN 1 THEN N' AND s.IsFlagged = 0 AND s.Status IN (1, 20)'
        WHEN 2 THEN N' AND s.IsFlagged = 0 AND s.Status IN (2, 7, 10, 14, 19)'
        WHEN 3 THEN N' AND s.IsFlagged = 0 AND s.Status IN (3, 4, 11, 12, 15, 16, 17)'
        WHEN 4 THEN CASE WHEN @ShowVoided = 1
                         THEN N' AND s.IsFlagged = 0 AND s.Status IN (6, 13, 18)'
                         ELSE N' AND s.IsFlagged = 0 AND s.Status IN (6, 13)'
                    END
        WHEN 5 THEN N' AND s.IsFlagged = 0 AND s.Status IN (8, 9)'
        WHEN 6 THEN N' AND s.IsFlagged = 0 AND s.Status = 5'
        WHEN 7 THEN N' AND s.IsFlagged = 1'
        ELSE N' AND 1 = 0'
    END;

    IF @ClaimNumber IS NOT NULL AND @ClaimNumber <> ''
        SET @FromWhereSql += N' AND s.ClaimNumber LIKE @ClaimNumber + N''%''';

    IF EXISTS (SELECT 1 FROM #PatientIdFilter)
        SET @FromWhereSql += N' AND s.ChildProfileId IN (SELECT Id FROM #PatientIdFilter)';

    IF EXISTS (SELECT 1 FROM #ClaimIdFilter)
        SET @FromWhereSql += N' AND s.ClaimId IN (SELECT Id FROM #ClaimIdFilter)';

    IF EXISTS (SELECT 1 FROM #ReasonIdFilter)
        SET @FromWhereSql += N' AND s.ReasonId IN (SELECT Id FROM #ReasonIdFilter)';

    IF EXISTS (SELECT 1 FROM #FunderIdFilter)
        SET @FromWhereSql += N' AND s.FunderId IN (SELECT Id FROM #FunderIdFilter)';

    IF EXISTS (SELECT 1 FROM #AssigneeIdFilter)
        SET @FromWhereSql += N' AND s.AssigneeId IN (SELECT Id FROM #AssigneeIdFilter)';

    IF EXISTS (SELECT 1 FROM #LocationIdFilter)
        SET @FromWhereSql += N' AND s.hcProviderLocationId IN (SELECT Id FROM #LocationIdFilter)';

    IF EXISTS (SELECT 1 FROM #RenderingProviderIdFilter)
        SET @FromWhereSql += N' AND (
            s.RenderingProviderId IN (SELECT Id FROM #RenderingProviderIdFilter)
            OR (
                s.ChargeRenderingProviderIdsCsv IS NOT NULL
                AND EXISTS (
                    SELECT 1
                    FROM #RenderingProviderIdFilter rf
                    WHERE CHARINDEX('','' + CAST(rf.Id AS VARCHAR(20)) + '','', '','' + s.ChargeRenderingProviderIdsCsv + '','') > 0
                )
            )
        )';

    IF @BalanceFrom IS NOT NULL
        SET @FromWhereSql += N' AND @BalanceFrom <= ISNULL(s.BilledAmount, 0) - ISNULL(s.FilterTotalPayment, 0)
            + ISNULL(s.FilterPositiveAdjustment, 0) - ISNULL(s.FilterNegativeAdjustment, 0)
            - ISNULL(s.FilterWriteOffAmount, 0)
            + ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)';

    IF @BalanceTo IS NOT NULL
        SET @FromWhereSql += N' AND @BalanceTo >= ISNULL(s.BilledAmount, 0) - ISNULL(s.FilterTotalPayment, 0)
            + ISNULL(s.FilterPositiveAdjustment, 0) - ISNULL(s.FilterNegativeAdjustment, 0)
            - ISNULL(s.FilterWriteOffAmount, 0)
            + ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)';

    IF @PatientResponsibilityFrom IS NOT NULL
        SET @FromWhereSql += N' AND @PatientResponsibilityFrom <= ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)';

    IF @PatientResponsibilityTo IS NOT NULL
        SET @FromWhereSql += N' AND @PatientResponsibilityTo >= ISNULL(s.FilterPositivePatientResp, 0) - ISNULL(s.FilterNegativePatientResp, 0)';

    IF @DateOfServiceFrom IS NOT NULL
        SET @FromWhereSql += N' AND s.DateOfServiceStart >= @DateOfServiceFrom';

    IF @DateOfServiceTo IS NOT NULL
        SET @FromWhereSql += N' AND s.DateOfServiceStart <= @DateOfServiceTo';

    IF @BilledFrom IS NOT NULL
        SET @FromWhereSql += N' AND @BilledFrom <= s.BilledAmount';

    IF @BilledTo IS NOT NULL
        SET @FromWhereSql += N' AND @BilledTo >= s.BilledAmount';

    IF EXISTS (SELECT 1 FROM #StatusIdFilter)
        SET @FromWhereSql += N' AND s.Status IN (SELECT Id FROM #StatusIdFilter)';

    IF EXISTS (SELECT 1 FROM #ValidationIdFilter)
        SET @FromWhereSql += N' AND (
            EXISTS (
                SELECT 1
                FROM #ValidationIdFilter vf
                WHERE vf.Severity <> @ShowWithNoErrorsIndicator
                  AND s.ValidationSeverityCsv IS NOT NULL
                  AND CHARINDEX('','' + CAST(vf.Severity AS VARCHAR(10)) + '','', '','' + s.ValidationSeverityCsv + '','') > 0
            )
            OR (@HasNoErrorValidation = 1 AND s.HasValidationErrors = 0)
        )';

    IF EXISTS (SELECT 1 FROM #ResponseIdFilter)
        SET @FromWhereSql += N' AND EXISTS (
            SELECT 1
            FROM dbo.ClearingHouseResponseDetails ch
            INNER JOIN #ResponseIdFilter rf ON rf.Id = ch.ResponseFileTypeId
            WHERE ch.ClaimId = s.ClaimId
              AND ch.DateDeleted IS NULL
        )';

    IF @ReasonCode IS NOT NULL AND @ReasonCode <> ''
        SET @FromWhereSql += N' AND s.ReasonCodes IS NOT NULL
            AND CHARINDEX('','' + @ReasonCode + '','', '','' + s.ReasonCodes + '','') > 0';

    DECLARE @OrderBySql NVARCHAR(400) = CASE
        WHEN @OrderField = N'claimNumber' AND @OrderDir = 0 THEN N's.ClaimNumber ASC, s.ClaimId ASC'
        WHEN @OrderField = N'claimNumber' AND @OrderDir = 1 THEN N's.ClaimNumber DESC, s.ClaimId DESC'
        WHEN @OrderField = N'DateOfServiceStart' AND @OrderDir = 0 THEN N's.DateOfServiceStart ASC, s.ClaimId ASC'
        WHEN @OrderField = N'DateOfServiceStart' AND @OrderDir = 1 THEN N's.DateOfServiceStart DESC, s.ClaimId DESC'
        WHEN @OrderField = N'DateOfServiceEnd' AND @OrderDir = 0 THEN N's.DateOfServiceEnd ASC, s.ClaimId ASC'
        WHEN @OrderField = N'DateOfServiceEnd' AND @OrderDir = 1 THEN N's.DateOfServiceEnd DESC, s.ClaimId DESC'
        WHEN @OrderField = N'BilledAmount' AND @OrderDir = 0 THEN N's.BilledAmount ASC, s.ClaimId ASC'
        WHEN @OrderField = N'BilledAmount' AND @OrderDir = 1 THEN N's.BilledAmount DESC, s.ClaimId DESC'
        WHEN @OrderField = N'PatientResponsibilityAmount' AND @OrderDir = 0 THEN N's.PatientResponsibilityAmount ASC, s.ClaimId ASC'
        WHEN @OrderField = N'PatientResponsibilityAmount' AND @OrderDir = 1 THEN N's.PatientResponsibilityAmount DESC, s.ClaimId DESC'
        WHEN @OrderField = N'ExpectedAmount' AND @OrderDir = 0 THEN N's.ExpectedAmount ASC, s.ClaimId ASC'
        WHEN @OrderField = N'ExpectedAmount' AND @OrderDir = 1 THEN N's.ExpectedAmount DESC, s.ClaimId DESC'
        WHEN @OrderField = N'PaymentAmount' AND @OrderDir = 0 THEN N's.PaymentAmount ASC, s.ClaimId ASC'
        WHEN @OrderField = N'PaymentAmount' AND @OrderDir = 1 THEN N's.PaymentAmount DESC, s.ClaimId DESC'
        WHEN @OrderField = N'BalanceAmount' AND @OrderDir = 0 THEN N's.BalanceAmount ASC, s.ClaimId ASC'
        WHEN @OrderField = N'BalanceAmount' AND @OrderDir = 1 THEN N's.BalanceAmount DESC, s.ClaimId DESC'
        WHEN @OrderField = N'Status' AND @OrderDir = 0 THEN N's.Status ASC, s.ClaimId ASC'
        WHEN @OrderField = N'Status' AND @OrderDir = 1 THEN N's.Status DESC, s.ClaimId DESC'
        WHEN @OrderField = N'PatientName' AND @OrderDir = 0 THEN N's.patientName ASC, s.ClaimId ASC'
        WHEN @OrderField = N'PatientName' AND @OrderDir = 1 THEN N's.patientName DESC, s.ClaimId DESC'
        WHEN @OrderField = N'FunderName' AND @OrderDir = 0 THEN N's.FunderName ASC, s.ClaimId ASC'
        WHEN @OrderField = N'FunderName' AND @OrderDir = 1 THEN N's.FunderName DESC, s.ClaimId DESC'
        WHEN @OrderField = N'AuthorizationNumber' AND @OrderDir = 0 THEN N's.AuthorizationNumber ASC, s.ClaimId ASC'
        WHEN @OrderField = N'AuthorizationNumber' AND @OrderDir = 1 THEN N's.AuthorizationNumber DESC, s.ClaimId DESC'
        WHEN @OrderField = N'PlaceOfService' AND @OrderDir = 0 THEN N's.PlaceOfService ASC, s.ClaimId ASC'
        WHEN @OrderField = N'PlaceOfService' AND @OrderDir = 1 THEN N's.PlaceOfService DESC, s.ClaimId DESC'
        WHEN @OrderField = N'RenderingProviderName' AND @OrderDir = 0 THEN N's.RenderingProviderName ASC, s.ClaimId ASC'
        WHEN @OrderField = N'RenderingProviderName' AND @OrderDir = 1 THEN N's.RenderingProviderName DESC, s.ClaimId DESC'
        WHEN @OrderField = N'BilledDate' AND @OrderDir = 0 THEN N's.BilledDate ASC, s.ClaimId ASC'
        WHEN @OrderField = N'BilledDate' AND @OrderDir = 1 THEN N's.BilledDate DESC, s.ClaimId DESC'
        ELSE N's.ClaimId DESC'
    END;

    DECLARE @SqlParams NVARCHAR(MAX) = N'
        @AccountInfoId INT,
        @ClaimNumber NVARCHAR(20),
        @BalanceFrom DECIMAL(18, 2),
        @BalanceTo DECIMAL(18, 2),
        @BilledFrom DECIMAL(18, 2),
        @BilledTo DECIMAL(18, 2),
        @PatientResponsibilityFrom INT,
        @PatientResponsibilityTo INT,
        @DateOfServiceFrom DATETIME,
        @DateOfServiceTo DATETIME,
        @ReasonCode NVARCHAR(100),
        @ShowWithNoErrorsIndicator INT,
        @HasNoErrorValidation BIT,
        @Skip INT,
        @TakeRows INT,
        @TotalCount INT OUTPUT';

    DECLARE @CountSql NVARCHAR(MAX) = N'
SELECT @TotalCount = COUNT(1)
' + @FromWhereSql + N'
OPTION (RECOMPILE);';

    EXEC sp_executesql
        @CountSql,
        @SqlParams,
        @AccountInfoId = @AccountInfoId,
        @ClaimNumber = @ClaimNumber,
        @BalanceFrom = @BalanceFrom,
        @BalanceTo = @BalanceTo,
        @BilledFrom = @BilledFrom,
        @BilledTo = @BilledTo,
        @PatientResponsibilityFrom = @PatientResponsibilityFrom,
        @PatientResponsibilityTo = @PatientResponsibilityTo,
        @DateOfServiceFrom = @DateOfServiceFrom,
        @DateOfServiceTo = @DateOfServiceTo,
        @ReasonCode = @ReasonCode,
        @ShowWithNoErrorsIndicator = @ShowWithNoErrorsIndicator,
        @HasNoErrorValidation = @HasNoErrorValidation,
        @Skip = @Skip,
        @TakeRows = @TakeRows,
        @TotalCount = @TotalCount OUTPUT;

    DECLARE @PageSql NVARCHAR(MAX) = N'
INSERT INTO #PageClaimIds (ClaimId)
SELECT s.ClaimId
' + @FromWhereSql + N'
ORDER BY ' + @OrderBySql + N'
OFFSET @Skip ROWS
FETCH NEXT @TakeRows ROWS ONLY
OPTION (RECOMPILE);';

    EXEC sp_executesql
        @PageSql,
        @SqlParams,
        @AccountInfoId = @AccountInfoId,
        @ClaimNumber = @ClaimNumber,
        @BalanceFrom = @BalanceFrom,
        @BalanceTo = @BalanceTo,
        @BilledFrom = @BilledFrom,
        @BilledTo = @BilledTo,
        @PatientResponsibilityFrom = @PatientResponsibilityFrom,
        @PatientResponsibilityTo = @PatientResponsibilityTo,
        @DateOfServiceFrom = @DateOfServiceFrom,
        @DateOfServiceTo = @DateOfServiceTo,
        @ReasonCode = @ReasonCode,
        @ShowWithNoErrorsIndicator = @ShowWithNoErrorsIndicator,
        @HasNoErrorValidation = @HasNoErrorValidation,
        @Skip = @Skip,
        @TakeRows = @TakeRows,
        @TotalCount = @TotalCount OUTPUT;

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
        @TotalCount AS totalCount
    FROM #PageClaimIds p
    INNER JOIN dbo.ClaimSummarySnapshot s ON s.ClaimId = p.ClaimId
    ORDER BY p.SortOrdinal;
END;
GO

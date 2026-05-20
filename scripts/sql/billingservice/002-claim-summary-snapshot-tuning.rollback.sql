/*
Rollback for ClaimSummarySnapshot browse indexes added by 002-claim-summary-snapshot-tuning.sql.

Restores the previous dbo.GetClaimsByAccountInfoId body that was replaced by the tuning script.
*/

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_FlaggedBrowse'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_FlaggedBrowse ON dbo.ClaimSummarySnapshot;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_CurrentBrowse'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_CurrentBrowse ON dbo.ClaimSummarySnapshot;
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

    DECLARE @ClaimIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ClaimIds IS NOT NULL AND @ClaimIds <> ''
        INSERT INTO @ClaimIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ClaimIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @PatientIdFilter TABLE (Id INT PRIMARY KEY);
    IF @PatientIds IS NOT NULL AND @PatientIds <> ''
        INSERT INTO @PatientIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@PatientIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @FunderIdFilter TABLE (Id INT PRIMARY KEY);
    IF @FunderIds IS NOT NULL AND @FunderIds <> ''
        INSERT INTO @FunderIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@FunderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @AssigneeIdFilter TABLE (Id INT PRIMARY KEY);
    IF @AssigneeIds IS NOT NULL AND @AssigneeIds <> ''
        INSERT INTO @AssigneeIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@AssigneeIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @LocationIdFilter TABLE (Id INT PRIMARY KEY);
    IF @LocationIds IS NOT NULL AND @LocationIds <> ''
        INSERT INTO @LocationIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@LocationIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @ReasonIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ReasonIds IS NOT NULL AND @ReasonIds <> ''
        INSERT INTO @ReasonIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ReasonIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @RenderingProviderIdFilter TABLE (Id INT PRIMARY KEY);
    IF @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> ''
        INSERT INTO @RenderingProviderIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@RenderingProviderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @StatusIdFilter TABLE (Id INT PRIMARY KEY);
    IF @StatusIds IS NOT NULL AND @StatusIds <> ''
        INSERT INTO @StatusIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@StatusIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @ValidationIdFilter TABLE (Severity INT PRIMARY KEY);
    IF @ValidationIds IS NOT NULL AND @ValidationIds <> ''
        INSERT INTO @ValidationIdFilter (Severity)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ValidationIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

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
    WHERE s.AccountInfoId = @AccountInfoId
      AND (
            (@Tab = 1 AND (s.Status = 1 OR s.Status = 20) AND s.IsFlagged = 0)
            OR (@Tab = 2 AND (s.Status = 2 OR s.Status = 7 OR s.Status = 10 OR s.Status = 14 OR s.Status = 19) AND s.IsFlagged = 0)
            OR (@Tab = 3 AND (s.Status = 3 OR s.Status = 4 OR s.Status = 11 OR s.Status = 12 OR s.Status = 15 OR s.Status = 16 OR s.Status = 17) AND s.IsFlagged = 0)
            OR (@Tab = 4 AND ((@ShowVoided = 1 AND s.Status = 18) OR s.Status = 6 OR s.Status = 13) AND s.IsFlagged = 0)
            OR (@Tab = 5 AND (s.Status = 8 OR s.Status = 9) AND s.IsFlagged = 0)
            OR (@Tab = 6 AND (s.Status = 5) AND s.IsFlagged = 0)
            OR (@Tab = 7 AND s.IsFlagged = 1)
          )
      AND s.ClaimNumber LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END
      AND (@PatientIds IS NULL OR @PatientIds = '' OR s.ChildProfileId IN (SELECT Id FROM @PatientIdFilter))
      AND (@ClaimIds IS NULL OR @ClaimIds = '' OR s.ClaimId IN (SELECT Id FROM @ClaimIdFilter))
      AND (@ReasonIds IS NULL OR @ReasonIds = '' OR s.ReasonId IN (SELECT Id FROM @ReasonIdFilter))
      AND (@FunderIds IS NULL OR @FunderIds = '' OR s.FunderId IN (SELECT Id FROM @FunderIdFilter))
      AND (@AssigneeIds IS NULL OR @AssigneeIds = '' OR s.AssigneeId IN (SELECT Id FROM @AssigneeIdFilter))
      AND (@LocationIds IS NULL OR @LocationIds = '' OR s.hcProviderLocationId IN (SELECT Id FROM @LocationIdFilter))
      AND (
            @RenderingProviderIds IS NULL
            OR @RenderingProviderIds = ''
            OR s.RenderingProviderId IN (SELECT Id FROM @RenderingProviderIdFilter)
            OR (
                s.ChargeRenderingProviderIdsCsv IS NOT NULL
                AND EXISTS (
                    SELECT 1
                    FROM @RenderingProviderIdFilter rf
                    WHERE CHARINDEX(',' + CAST(rf.Id AS VARCHAR(20)) + ',', ',' + s.ChargeRenderingProviderIdsCsv + ',') > 0
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
      AND (@StatusIds IS NULL OR @StatusIds = '' OR s.Status IN (SELECT Id FROM @StatusIdFilter))
      AND (
            @ValidationIds IS NULL
            OR @ValidationIds = ''
            OR EXISTS (
                SELECT 1
                FROM @ValidationIdFilter vf
                WHERE vf.Severity <> @ShowWithNoErrorsIndicator
                  AND s.ValidationSeverityCsv IS NOT NULL
                  AND CHARINDEX(',' + CAST(vf.Severity AS VARCHAR(10)) + ',', ',' + s.ValidationSeverityCsv + ',') > 0
            )
            OR (
                @ShowWithNoErrorsIndicator IS NOT NULL
                AND @ValidationIds IS NOT NULL
                AND CHARINDEX(CAST(@ShowWithNoErrorsIndicator AS VARCHAR(10)), @ValidationIds) > 0
                AND s.HasValidationErrors = 0
            )
          )
      AND (
            @ResponseIds IS NULL
            OR @ResponseIds = ''
            OR EXISTS (
                SELECT *
                FROM dbo.ClearingHouseResponseDetails ch
                WHERE ch.ClaimId = s.ClaimId
                  AND CAST(LTRIM(RTRIM(@ResponseIds)) AS VARCHAR(100)) IN (
                      SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@ResponseIds, ',')
                  )
            )
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
    FETCH NEXT CASE WHEN @Take = 0 THEN 2147483647 ELSE @Take END ROWS ONLY;
END
GO

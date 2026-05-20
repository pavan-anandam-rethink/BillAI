/*
Optimization for dbo.GetClaimsByAccountInfoId:
- Keeps result shape and filtering behavior.
- Uses snapshot-focused indexes and recompilation for parameter-sensitive plans.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_Account_TabStatus_Dos_Claim'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_TabStatus_Dos_Claim
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, DateOfServiceStart, ClaimId)
    INCLUDE (
        ClaimNumber,
        ChildProfileId,
        ReasonId,
        FunderId,
        AssigneeId,
        hcProviderLocationId,
        RenderingProviderId,
        BilledAmount,
        FilterTotalPayment,
        FilterPositiveAdjustment,
        FilterNegativeAdjustment,
        FilterWriteOffAmount,
        FilterPositivePatientResp,
        FilterNegativePatientResp,
        PatientResponsibilityAmount,
        ExpectedAmount,
        PaymentAmount,
        BalanceAmount,
        BilledDate,
        HasValidationErrors,
        ValidationSeverityCsv,
        ChargeRenderingProviderIdsCsv,
        AuthorizationNumber,
        PlaceOfService,
        patientName,
        FunderName,
        RenderingProviderName
    )
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_Account_TabStatus_ClaimNumber_Claim'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_Account_TabStatus_ClaimNumber_Claim
    ON dbo.ClaimSummarySnapshot (AccountInfoId, IsFlagged, Status, ClaimNumber, ClaimId)
    INCLUDE (
        DateOfServiceStart,
        DateOfServiceEnd,
        ChildProfileId,
        ReasonId,
        FunderId,
        AssigneeId,
        hcProviderLocationId,
        RenderingProviderId,
        BilledAmount,
        FilterTotalPayment,
        FilterPositiveAdjustment,
        FilterNegativeAdjustment,
        FilterWriteOffAmount,
        FilterPositivePatientResp,
        FilterNegativePatientResp,
        PatientResponsibilityAmount,
        ExpectedAmount,
        PaymentAmount,
        BalanceAmount,
        BilledDate
    )
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClearingHouseResponseDetails_ClaimId_ResponseFileTypeId_DateDeleted'
      AND object_id = OBJECT_ID(N'dbo.ClearingHouseResponseDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClearingHouseResponseDetails_ClaimId_ResponseFileTypeId_DateDeleted
    ON dbo.ClearingHouseResponseDetails (ClaimId, ResponseFileTypeId, DateDeleted)
    INCLUDE (Id)
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
    DECLARE @ApplyClaimFilter BIT = CASE WHEN @ClaimIds IS NOT NULL AND @ClaimIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyPatientFilter BIT = CASE WHEN @PatientIds IS NOT NULL AND @PatientIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyFunderFilter BIT = CASE WHEN @FunderIds IS NOT NULL AND @FunderIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyAssigneeFilter BIT = CASE WHEN @AssigneeIds IS NOT NULL AND @AssigneeIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyLocationFilter BIT = CASE WHEN @LocationIds IS NOT NULL AND @LocationIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyReasonFilter BIT = CASE WHEN @ReasonIds IS NOT NULL AND @ReasonIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyRenderingProviderFilter BIT = CASE WHEN @RenderingProviderIds IS NOT NULL AND @RenderingProviderIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyStatusFilter BIT = CASE WHEN @StatusIds IS NOT NULL AND @StatusIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyValidationFilter BIT = CASE WHEN @ValidationIds IS NOT NULL AND @ValidationIds <> '' THEN 1 ELSE 0 END;
    DECLARE @ApplyResponseFilter BIT = CASE WHEN @ResponseIds IS NOT NULL AND @ResponseIds <> '' THEN 1 ELSE 0 END;

    DECLARE @ClaimIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyClaimFilter = 1
        INSERT INTO @ClaimIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ClaimIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @PatientIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyPatientFilter = 1
        INSERT INTO @PatientIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@PatientIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @FunderIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyFunderFilter = 1
        INSERT INTO @FunderIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@FunderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @AssigneeIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyAssigneeFilter = 1
        INSERT INTO @AssigneeIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@AssigneeIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @LocationIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyLocationFilter = 1
        INSERT INTO @LocationIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@LocationIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @ReasonIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyReasonFilter = 1
        INSERT INTO @ReasonIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@ReasonIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @RenderingProviderIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyRenderingProviderFilter = 1
        INSERT INTO @RenderingProviderIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@RenderingProviderIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @StatusIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyStatusFilter = 1
        INSERT INTO @StatusIdFilter (Id)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(@StatusIds, ',')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

    DECLARE @ValidationIdFilter TABLE (Severity INT PRIMARY KEY);
    IF @ApplyValidationFilter = 1
        INSERT INTO @ValidationIdFilter (Severity)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ValidationIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    DECLARE @ResponseIdFilter TABLE (Id INT PRIMARY KEY);
    IF @ApplyResponseFilter = 1
        INSERT INTO @ResponseIdFilter (Id)
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
        FROM STRING_SPLIT(@ResponseIds, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;

    DECLARE @IncludeNoValidationErrors BIT = CASE
        WHEN EXISTS (
            SELECT 1
            FROM @ValidationIdFilter
            WHERE Severity = @ShowWithNoErrorsIndicator
        ) THEN 1 ELSE 0
    END;

    DECLARE @TabStatus TABLE (Status INT PRIMARY KEY);
    IF @Tab = 1
    BEGIN
        INSERT INTO @TabStatus (Status) VALUES (1), (20);
    END;
    ELSE IF @Tab = 2
    BEGIN
        INSERT INTO @TabStatus (Status) VALUES (2), (7), (10), (14), (19);
    END;
    ELSE IF @Tab = 3
    BEGIN
        INSERT INTO @TabStatus (Status) VALUES (3), (4), (11), (12), (15), (16), (17);
    END;
    ELSE IF @Tab = 4
    BEGIN
        INSERT INTO @TabStatus (Status) VALUES (6), (13);
        IF @ShowVoided = 1
            INSERT INTO @TabStatus (Status) VALUES (18);
    END;
    ELSE IF @Tab = 5
    BEGIN
        INSERT INTO @TabStatus (Status) VALUES (8), (9);
    END;
    ELSE IF @Tab = 6
    BEGIN
        INSERT INTO @TabStatus (Status) VALUES (5);
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
            BalanceForFilter =
                ISNULL(s.BilledAmount, 0)
                - ISNULL(s.FilterTotalPayment, 0)
                + ISNULL(s.FilterPositiveAdjustment, 0)
                - ISNULL(s.FilterNegativeAdjustment, 0)
                - ISNULL(s.FilterWriteOffAmount, 0)
                + ISNULL(s.FilterPositivePatientResp, 0)
                - ISNULL(s.FilterNegativePatientResp, 0),
            PatientRespForFilter =
                ISNULL(s.FilterPositivePatientResp, 0)
                - ISNULL(s.FilterNegativePatientResp, 0)
    ) f
    WHERE s.AccountInfoId = @AccountInfoId
      AND (
            (@Tab = 7 AND s.IsFlagged = 1)
            OR (
                @Tab <> 7
                AND s.IsFlagged = 0
                AND EXISTS (SELECT 1 FROM @TabStatus ts WHERE ts.Status = s.Status)
            )
      )
      AND s.ClaimNumber LIKE CASE WHEN @ClaimNumber IS NOT NULL THEN @ClaimNumber + '%' ELSE '%' END
      AND (@ApplyPatientFilter = 0 OR s.ChildProfileId IN (SELECT Id FROM @PatientIdFilter))
      AND (@ApplyClaimFilter = 0 OR s.ClaimId IN (SELECT Id FROM @ClaimIdFilter))
      AND (@ApplyReasonFilter = 0 OR s.ReasonId IN (SELECT Id FROM @ReasonIdFilter))
      AND (@ApplyFunderFilter = 0 OR s.FunderId IN (SELECT Id FROM @FunderIdFilter))
      AND (@ApplyAssigneeFilter = 0 OR s.AssigneeId IN (SELECT Id FROM @AssigneeIdFilter))
      AND (@ApplyLocationFilter = 0 OR s.hcProviderLocationId IN (SELECT Id FROM @LocationIdFilter))
      AND (
            @ApplyRenderingProviderFilter = 0
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
      AND (@BalanceFrom IS NULL OR @BalanceFrom <= f.BalanceForFilter)
      AND (@BalanceTo IS NULL OR @BalanceTo >= f.BalanceForFilter)
      AND (@PatientResponsibilityFrom IS NULL OR @PatientResponsibilityFrom <= f.PatientRespForFilter)
      AND (@PatientResponsibilityTo IS NULL OR @PatientResponsibilityTo >= f.PatientRespForFilter)
      AND (@DateOfServiceFrom IS NULL OR s.DateOfServiceStart >= @DateOfServiceFrom)
      AND (@DateOfServiceTo IS NULL OR s.DateOfServiceStart <= @DateOfServiceTo)
      AND (@BilledFrom IS NULL OR @BilledFrom <= s.BilledAmount)
      AND (@BilledTo IS NULL OR @BilledTo >= s.BilledAmount)
      AND (@ApplyStatusFilter = 0 OR s.Status IN (SELECT Id FROM @StatusIdFilter))
      AND (
            @ApplyValidationFilter = 0
            OR EXISTS (
                SELECT 1
                FROM @ValidationIdFilter vf
                WHERE vf.Severity <> @ShowWithNoErrorsIndicator
                  AND s.ValidationSeverityCsv IS NOT NULL
                  AND CHARINDEX(',' + CAST(vf.Severity AS VARCHAR(10)) + ',', ',' + s.ValidationSeverityCsv + ',') > 0
            )
            OR (@IncludeNoValidationErrors = 1 AND s.HasValidationErrors = 0)
          )
      AND (
            @ApplyResponseFilter = 0
            OR EXISTS (
                SELECT 1
                FROM dbo.ClearingHouseResponseDetails ch
                WHERE ch.ClaimId = s.ClaimId
                  AND ch.DateDeleted IS NULL
                  AND ch.ResponseFileTypeId IN (SELECT Id FROM @ResponseIdFilter)
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
    FETCH NEXT CASE WHEN @Take = 0 THEN 2147483647 ELSE @Take END ROWS ONLY
    OPTION (RECOMPILE);
END;
GO

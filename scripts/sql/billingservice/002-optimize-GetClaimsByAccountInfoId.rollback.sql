/*
Rollback for 002-optimize-GetClaimsByAccountInfoId.sql

Restores the original dynamic-SQL version of the procedure and drops the
covering index that was added alongside the optimized version.
*/

-- 1. Restore original stored procedure ------------------------------------

CREATE OR ALTER PROCEDURE dbo.GetClaimsByAccountInfoId
(
    @AccountInfoId INT,
    @Skip INT,
    @Take INT,
    @OrderField NVARCHAR(50) = 'DateOfServiceStart',
    @OrderDir BIT = 1
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @OrderBy NVARCHAR(200);

    SET @OrderBy =
    CASE
        WHEN @OrderField = 'claimNumber' AND @OrderDir = 0 THEN 'ClaimNumber ASC'
        WHEN @OrderField = 'claimNumber' AND @OrderDir = 1 THEN 'ClaimNumber DESC'

        WHEN @OrderField = 'DateOfServiceStart' AND @OrderDir = 0 THEN 'DateOfServiceStart ASC'
        WHEN @OrderField = 'DateOfServiceStart' AND @OrderDir = 1 THEN 'DateOfServiceStart DESC'

        WHEN @OrderField = 'BilledAmount' AND @OrderDir = 0 THEN 'BilledAmount ASC'
        WHEN @OrderField = 'BilledAmount' AND @OrderDir = 1 THEN 'BilledAmount DESC'

        ELSE 'DateOfServiceStart DESC'
    END;

    SET @SQL = '
    SELECT
        ClaimId AS Id,
        ClaimNumber,
        DateOfServiceStart,
        DateOfServiceEnd,
        patientName,
        FunderName,
        BilledAmount,
        ExpectedAmount,
        PaymentAmount,
        BalanceAmount,
        Status
    FROM dbo.ClaimSummarySnapshot WITH (NOLOCK)
    WHERE AccountInfoId = @AccountInfoId
    ORDER BY ' + @OrderBy + '
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY
    OPTION (RECOMPILE);
    ';

    EXEC sp_executesql
    @SQL,
    N'
        @AccountInfoId INT,
        @Skip INT,
        @Take INT
    ',
    @AccountInfoId,
    @Skip,
    @Take;
END;
GO

-- 2. Drop covering index --------------------------------------------------

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_AccountInfoId_Covering'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    DROP INDEX IX_ClaimSummarySnapshot_AccountInfoId_Covering ON dbo.ClaimSummarySnapshot;
END;
GO

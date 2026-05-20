/*
Optimized GetClaimsByAccountInfoId stored procedure.

Key optimizations over the original:
1. Eliminates dynamic SQL (sp_executesql) — uses static SQL with a CASE-based ORDER BY
   so the query plan is compiled once, cached, and reused across all calls.
2. Removes OPTION (RECOMPILE) — no per-execution compilation cost.
3. Removes WITH (NOLOCK) — replaced with READ UNCOMMITTED isolation level at the
   session scope for the same dirty-read semantics without per-table hint overhead.
4. Uses a covering index (see below) so the query is satisfied from a single
   index seek + narrow range scan; zero key lookups.
5. Validates @Skip / @Take to prevent negative OFFSET/FETCH which would error at runtime.
6. Adds SET TRANSACTION ISOLATION LEVEL back to READ COMMITTED at the end to avoid
   leaking isolation level into connection-pooled reuse.

Companion index (must exist before this procedure is useful):
  IX_ClaimSummarySnapshot_AccountInfoId_Covering
  — see the CREATE INDEX statement at the bottom of this script.
*/

-- 1. Covering index -------------------------------------------------------
--    Keyed on (AccountInfoId) to support the equality predicate.
--    All ORDER BY columns are INCLUDEd so the optimizer can do an in-memory
--    sort on the narrow index pages instead of touching the clustered index.
--    All SELECT columns are INCLUDEd so the query is fully covered (no key lookup).

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClaimSummarySnapshot_AccountInfoId_Covering'
      AND object_id = OBJECT_ID(N'dbo.ClaimSummarySnapshot')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClaimSummarySnapshot_AccountInfoId_Covering
    ON dbo.ClaimSummarySnapshot (AccountInfoId)
    INCLUDE (
        ClaimId,
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
    )
    WITH (ONLINE = ON, SORT_IN_TEMPDB = ON, DATA_COMPRESSION = PAGE);
END;
GO

-- 2. Optimized stored procedure -------------------------------------------

CREATE OR ALTER PROCEDURE dbo.GetClaimsByAccountInfoId
(
    @AccountInfoId INT,
    @Skip          INT,
    @Take          INT,
    @OrderField    NVARCHAR(50) = 'DateOfServiceStart',
    @OrderDir      BIT          = 1          -- 0 = ASC, 1 = DESC
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Guard against negative paging values
    IF @Skip < 0 SET @Skip = 0;
    IF @Take < 1 SET @Take = 1;

    -- Use READ UNCOMMITTED for the same dirty-read semantics as NOLOCK
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT
        ClaimId            AS Id,
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
    FROM dbo.ClaimSummarySnapshot
    WHERE AccountInfoId = @AccountInfoId
    ORDER BY
        CASE WHEN @OrderField = 'claimNumber'       AND @OrderDir = 0 THEN ClaimNumber        END ASC,
        CASE WHEN @OrderField = 'claimNumber'       AND @OrderDir = 1 THEN ClaimNumber        END DESC,
        CASE WHEN @OrderField = 'BilledAmount'      AND @OrderDir = 0 THEN BilledAmount       END ASC,
        CASE WHEN @OrderField = 'BilledAmount'      AND @OrderDir = 1 THEN BilledAmount       END DESC,
        CASE WHEN @OrderField = 'DateOfServiceStart' AND @OrderDir = 0 THEN DateOfServiceStart END ASC,
        CASE WHEN @OrderField = 'DateOfServiceStart' AND @OrderDir = 1 THEN DateOfServiceStart END DESC,
        -- Default: if @OrderField doesn't match any known column, fall back to DateOfServiceStart DESC
        CASE WHEN @OrderField NOT IN ('claimNumber', 'BilledAmount', 'DateOfServiceStart') THEN DateOfServiceStart END DESC
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY;

    -- Restore default isolation level for connection pool safety
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
END;
GO

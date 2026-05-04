-- =============================================
-- Rollback Script
-- Branch:   RHD-13893-integrated-billing-reporting-financial-summary-report-monthly-funder-new-changes
--
-- Stored Procedures:
--   1. usp_FunderFinancialSummary
--   2. usp_MonthlyFinancialSummary
--   3. usp_CalculateFunderStartingAR
--   4. usp_CalculateStartingAR
--
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

PRINT '=== Starting Rollback for RHD-13893 Stored Procedures ==='
PRINT ''
PRINT 'Execution Time: ' + CONVERT(NVARCHAR(30), GETDATE(), 121)
PRINT ''

------------------------------------------------------------
-- 1. usp_FunderFinancialSummary
--    (Caller — depends on usp_CalculateFunderStartingAR)
------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_FunderFinancialSummary]')
      AND type = N'P'
)
BEGIN
    DROP PROCEDURE [dbo].[usp_FunderFinancialSummary];
    PRINT '1. [dbo].[usp_FunderFinancialSummary]          -> DROPPED';
END
ELSE
BEGIN
    PRINT '1. [dbo].[usp_FunderFinancialSummary]          -> DOES NOT EXIST (Skipped)';
END
GO

------------------------------------------------------------
-- 2. usp_MonthlyFinancialSummary
--    (Caller — depends on usp_CalculateStartingAR)
------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_MonthlyFinancialSummary]')
      AND type = N'P'
)
BEGIN
    DROP PROCEDURE [dbo].[usp_MonthlyFinancialSummary];
    PRINT '2. [dbo].[usp_MonthlyFinancialSummary]         -> DROPPED';
END
ELSE
BEGIN
    PRINT '2. [dbo].[usp_MonthlyFinancialSummary]         -> DOES NOT EXIST (Skipped)';
END
GO

------------------------------------------------------------
-- 3. usp_CalculateFunderStartingAR
--    (Callee — used by usp_FunderFinancialSummary)
------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_CalculateFunderStartingAR]')
      AND type = N'P'
)
BEGIN
    DROP PROCEDURE [dbo].[usp_CalculateFunderStartingAR];
    PRINT '3. [dbo].[usp_CalculateFunderStartingAR]       -> DROPPED';
END
ELSE
BEGIN
    PRINT '3. [dbo].[usp_CalculateFunderStartingAR]       -> DOES NOT EXIST (Skipped)';
END
GO

------------------------------------------------------------
-- 4. usp_CalculateStartingAR
--    (Callee — used by usp_MonthlyFinancialSummary)
------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[usp_CalculateStartingAR]')
      AND type = N'P'
)
BEGIN
    DROP PROCEDURE [dbo].[usp_CalculateStartingAR];
    PRINT '4. [dbo].[usp_CalculateStartingAR]             -> DROPPED';
END
ELSE
BEGIN
    PRINT '4. [dbo].[usp_CalculateStartingAR]             -> DOES NOT EXIST (Skipped)';
END
GO

------------------------------------------------------------
-- Verification: Confirm none of the procedures exist
------------------------------------------------------------
PRINT ''
PRINT '=== Verification ==='

IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id IN (
        OBJECT_ID(N'[dbo].[usp_FunderFinancialSummary]'),
        OBJECT_ID(N'[dbo].[usp_MonthlyFinancialSummary]'),
        OBJECT_ID(N'[dbo].[usp_CalculateFunderStartingAR]'),
        OBJECT_ID(N'[dbo].[usp_CalculateStartingAR]')
    )
    AND type = N'P'
)
BEGIN
    PRINT 'PASS: All 4 stored procedures have been removed successfully.';
END
ELSE
BEGIN
    PRINT 'WARNING: One or more stored procedures still exist!';
    SELECT
        name AS ProcedureName,
        create_date AS CreatedDate,
        modify_date AS ModifiedDate
    FROM sys.objects
    WHERE object_id IN (
        OBJECT_ID(N'[dbo].[usp_FunderFinancialSummary]'),
        OBJECT_ID(N'[dbo].[usp_MonthlyFinancialSummary]'),
        OBJECT_ID(N'[dbo].[usp_CalculateFunderStartingAR]'),
        OBJECT_ID(N'[dbo].[usp_CalculateStartingAR]')
    )
    AND type = N'P';
END
GO

PRINT ''
PRINT 'Completion Time: ' + CONVERT(NVARCHAR(30), GETDATE(), 121)
PRINT '=== Rollback Complete ==='
GO

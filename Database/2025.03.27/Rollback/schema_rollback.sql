SET NOEXEC OFF
SET NOCOUNT ON
SET NUMERIC_ROUNDABORT OFF
GO

SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO

SET XACT_ABORT ON
GO

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE

BEGIN TRANSACTION

IF @@ERROR <> 0 SET NOEXEC ON

GO

--Rollback script to change the datatype back to INT
ALTER TABLE paymentClaim 
ALTER COLUMN patientId INT;
ALTER TABLE paymentClaim 
ALTER COLUMN renderingProviderId INT;

-- Rollback Script for dropping column with constraints

-- Drop Foreign Key Constraint if it exists
IF EXISTS (
   SELECT 1
   FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
   WHERE TABLE_NAME = 'paymentclaimservicelineadjustment'
   AND CONSTRAINT_NAME = 'FK_claimActionType'
)
BEGIN
   ALTER TABLE paymentclaimservicelineadjustment  
   DROP CONSTRAINT FK_claimActionType;
END
-- Drop Default Constraint if it exists
DECLARE @ConstraintName NVARCHAR(200);
SELECT @ConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID('paymentclaimservicelineadjustment')
AND c.name = 'claimActionTypeId';
IF @ConstraintName IS NOT NULL
BEGIN
   EXEC('ALTER TABLE paymentclaimservicelineadjustment DROP CONSTRAINT ' + @ConstraintName);
END
-- Drop Column if it exists
IF EXISTS (
   SELECT 1
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'paymentclaimservicelineadjustment'
   AND COLUMN_NAME = 'claimActionTypeId'
)
BEGIN
   ALTER TABLE paymentclaimservicelineadjustment  
   DROP COLUMN claimActionTypeId;
END
GO

IF @@ERROR <> 0 SET NOEXEC ON
GO

COMMIT TRANSACTION
GO

DECLARE @Success AS BIT
SET @Success=1
SET NOEXEC OFF
IF (@Success = 1) PRINT 'Database update succeeded'
ELSE BEGIN
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
	PRINT 'The database update failed. ROLLBACK TRANSACTION'
END
GO

----###############################################
------ 257311 Searchable Adjustment Codes
----################################################

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PaymentAdjustmentReason'
      AND TABLE_SCHEMA = 'dbo'
      AND COLUMN_NAME = 'IsDefault'
)
BEGIN
		ALTER TABLE [dbo].[PaymentAdjustmentReason]
		ADD IsDefault BIT NOT NULL 
		CONSTRAINT DF__PaymentAdjustmentReason__Default__IsDefault DEFAULT 0;
END
GO

BEGIN TRY
    BEGIN TRANSACTION;
    -- Update default values
    UPDATE PaymentAdjustmentReason
    SET IsDefault = 1
    WHERE IsDefault = 0
      AND (
            (GroupCode = 'CO' AND AdjustmentCode = '45') OR
            (GroupCode = 'CO' AND AdjustmentCode = '42') OR
            (GroupCode = 'CO' AND AdjustmentCode = '50') OR
            (GroupCode = 'PR' AND AdjustmentCode = '1')  OR
            (GroupCode = 'PR' AND AdjustmentCode = '2')  OR
            (GroupCode = 'PR' AND AdjustmentCode = '3')  OR
            (GroupCode = 'PR' AND AdjustmentCode = '49') OR
            (GroupCode = 'PR' AND AdjustmentCode = '50') OR
            (GroupCode = 'OA' AND AdjustmentCode = '23') OR
            (GroupCode = 'OA' AND AdjustmentCode = '22') OR
            (GroupCode = 'OA' AND AdjustmentCode = '94')
      );

    COMMIT TRANSACTION;
	SELECT 'TRANSACTION COMPLETED'
	
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
	BEGIN
        ROLLBACK TRANSACTION;
		SELECT 'ROLLBACK COMPLETED'
	END

    SELECT 'Error occurred: ' + ERROR_MESSAGE();
    --THROW;
END CATCH;

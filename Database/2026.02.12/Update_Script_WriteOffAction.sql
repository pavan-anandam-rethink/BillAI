------------------------------------------------------
-- Script: Update descriptions in WriteOffAction
------------------------------------------------------

-- ========== ID 1 ==========
IF EXISTS (SELECT 1 FROM [dbo].[WriteOffAction] WHERE id = 1)
BEGIN
    UPDATE [dbo].[WriteOffAction]
    SET description = 'Remaining Amount'
    WHERE id = 1;
END

-- ========== ID 3 ==========
IF EXISTS (SELECT 1 FROM [dbo].[WriteOffAction] WHERE id = 3)
BEGIN
    UPDATE [dbo].[WriteOffAction]
    SET description = 'Other Amount'
    WHERE id = 3;
END

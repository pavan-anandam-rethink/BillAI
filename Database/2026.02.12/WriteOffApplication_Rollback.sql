------------------------------------------------------
-- Rollback: Restore previous descriptions
------------------------------------------------------

-- ID 1
IF EXISTS (SELECT 1 FROM [dbo].[WriteOffApplication] WHERE id = 1)
BEGIN
    UPDATE [dbo].[WriteOffApplication]
    SET description = 'Newest procedures first'
    WHERE id = 1;
END

-- ID 2
IF EXISTS (SELECT 1 FROM [dbo].[WriteOffApplication] WHERE id = 2)
BEGIN
    UPDATE [dbo].[WriteOffApplication]
    SET description = 'Oldest procedure first'
    WHERE id = 2;
END

-- ID 3
IF EXISTS (SELECT 1 FROM [dbo].[WriteOffApplication] WHERE id = 3)
BEGIN
    UPDATE [dbo].[WriteOffApplication]
    SET description = 'Highest Balance first'
    WHERE id = 3;
END

-- ID 4
IF EXISTS (SELECT 1 FROM [dbo].[WriteOffApplication] WHERE id = 4)
BEGIN
    UPDATE [dbo].[WriteOffApplication]
    SET description = 'Lowest Balance first'
    WHERE id = 4;
END

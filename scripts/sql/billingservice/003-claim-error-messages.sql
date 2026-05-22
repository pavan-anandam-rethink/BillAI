DECLARE @defaultCategoryId INT = (SELECT TOP 1 Id FROM dbo.ClaimErrorCategories ORDER BY Id);
DECLARE @billingProviderCategoryId INT = (SELECT TOP 1 Id FROM dbo.ClaimErrorCategories WHERE Prefix = 'BP');
DECLARE @funderCategoryId INT = (SELECT TOP 1 Id FROM dbo.ClaimErrorCategories WHERE Prefix = 'FND');

SET @billingProviderCategoryId = COALESCE(@billingProviderCategoryId, @defaultCategoryId);
SET @funderCategoryId = COALESCE(@funderCategoryId, @defaultCategoryId);

-- Update Billing Provider Zip description (1509)
IF EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE ErrorNumber = 1509)
BEGIN
    UPDATE dbo.ClaimErrorMessages
    SET ShortDescription = 'Zip code missing or invalid.',
        LongDescription = 'Zip code missing or invalid. Please verify that the zip code is present, contains only numeric values and includes the 4 digit zip extension.',
        DateLastModified = GETUTCDATE()
    WHERE ErrorNumber = 1509;
END
ELSE
BEGIN
    INSERT INTO dbo.ClaimErrorMessages
        (ShortDescription, LongDescription, ErrorNumber, Severity, ClaimErrorCategoryId, CreatedBy, DateCreated)
    VALUES
        ('Zip code missing or invalid.',
         'Zip code missing or invalid. Please verify that the zip code is present, contains only numeric values and includes the 4 digit zip extension.',
         1509, 2, @billingProviderCategoryId, 0, GETUTCDATE());
END

-- Eligibility Verification Alert (3213)
IF EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE ErrorNumber = 3213)
BEGIN
    UPDATE dbo.ClaimErrorMessages
    SET ShortDescription = 'Eligibility verification overdue.',
        LongDescription = 'Eligibility has not been verify in over 31 days since this claim has been created. Running a new eligibility check prior to submission may help avoid claim delays.',
        Severity = 3,
        ClaimErrorCategoryId = @funderCategoryId,
        DateLastModified = GETUTCDATE()
    WHERE ErrorNumber = 3213;
END
ELSE
BEGIN
    INSERT INTO dbo.ClaimErrorMessages
        (ShortDescription, LongDescription, ErrorNumber, Severity, ClaimErrorCategoryId, CreatedBy, DateCreated)
    VALUES
        ('Eligibility verification overdue.',
         'Eligibility has not been verify in over 31 days since this claim has been created. Running a new eligibility check prior to submission may help avoid claim delays.',
         3213, 3, @funderCategoryId, 0, GETUTCDATE());
END

-- Claim Enrollment Required (3214)
IF EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE ErrorNumber = 3214)
BEGIN
    UPDATE dbo.ClaimErrorMessages
    SET ShortDescription = 'Claim enrollment required.',
        LongDescription = 'Claim enrollment must be completed prior to claim submission for this payer.',
        Severity = 2,
        ClaimErrorCategoryId = @funderCategoryId,
        DateLastModified = GETUTCDATE()
    WHERE ErrorNumber = 3214;
END
ELSE
BEGIN
    INSERT INTO dbo.ClaimErrorMessages
        (ShortDescription, LongDescription, ErrorNumber, Severity, ClaimErrorCategoryId, CreatedBy, DateCreated)
    VALUES
        ('Claim enrollment required.',
         'Claim enrollment must be completed prior to claim submission for this payer.',
         3214, 2, @funderCategoryId, 0, GETUTCDATE());
END

-- AI Rules Engine Error (3215)
IF EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE ErrorNumber = 3215)
BEGIN
    UPDATE dbo.ClaimErrorMessages
    SET ShortDescription = 'AI rules engine error.',
        LongDescription = 'AI rules engine validation failed for this claim.',
        Severity = 2,
        ClaimErrorCategoryId = @defaultCategoryId,
        DateLastModified = GETUTCDATE()
    WHERE ErrorNumber = 3215;
END
ELSE
BEGIN
    INSERT INTO dbo.ClaimErrorMessages
        (ShortDescription, LongDescription, ErrorNumber, Severity, ClaimErrorCategoryId, CreatedBy, DateCreated)
    VALUES
        ('AI rules engine error.',
         'AI rules engine validation failed for this claim.',
         3215, 2, @defaultCategoryId, 0, GETUTCDATE());
END

-- AI Rules Engine Warning (3216)
IF EXISTS (SELECT 1 FROM dbo.ClaimErrorMessages WHERE ErrorNumber = 3216)
BEGIN
    UPDATE dbo.ClaimErrorMessages
    SET ShortDescription = 'AI rules engine warning.',
        LongDescription = 'AI rules engine flagged a warning for this claim.',
        Severity = 3,
        ClaimErrorCategoryId = @defaultCategoryId,
        DateLastModified = GETUTCDATE()
    WHERE ErrorNumber = 3216;
END
ELSE
BEGIN
    INSERT INTO dbo.ClaimErrorMessages
        (ShortDescription, LongDescription, ErrorNumber, Severity, ClaimErrorCategoryId, CreatedBy, DateCreated)
    VALUES
        ('AI rules engine warning.',
         'AI rules engine flagged a warning for this claim.',
         3216, 3, @defaultCategoryId, 0, GETUTCDATE());
END

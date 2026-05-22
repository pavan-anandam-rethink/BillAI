DELETE FROM dbo.ClaimErrorMessages WHERE ErrorNumber IN (3213, 3214, 3215, 3216);

-- Revert billing provider zip description to legacy text if needed
UPDATE dbo.ClaimErrorMessages
SET ShortDescription = 'Zip code missing or invalid.',
    LongDescription = 'Zip code missing or invalid.'
WHERE ErrorNumber = 1509;

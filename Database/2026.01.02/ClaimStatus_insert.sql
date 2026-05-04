

IF NOT EXISTS (SELECT 1 FROM claimStatus WHERE id = 20)
BEGIN
    INSERT INTO claimStatus (id, name, dateCreated, dateLastModified)
    VALUES (20, 'Approval Failed', GETDATE(), GETDATE())
END

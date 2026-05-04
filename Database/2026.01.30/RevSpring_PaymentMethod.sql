IF NOT EXISTS (SELECT 1 FROM PaymentMethod WHERE Id = 9)
BEGIN
    INSERT INTO PaymentMethod (Id, Name, DateCreated, DateLastModified)
    VALUES (9, 'RevSpring', GETDATE(), GETDATE());
END



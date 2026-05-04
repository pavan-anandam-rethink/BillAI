----########################################################
------ Old Records MODIFY Scripts 
----########################################################
BEGIN TRANSACTION;

UPDATE PatientInvoice
SET statusId = 2;


COMMIT TRANSACTION;



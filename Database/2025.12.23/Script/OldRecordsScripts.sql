----########################################################
------ Old Records MODIFY Scripts 
----########################################################
BEGIN TRANSACTION;

;WITH AggregatedRecords AS (
    SELECT 
        pi.id,
        SUM(pid.adjustmentPR) AS adjustmentPR,
        SUM(pid.patientPayments) AS patientPayments,
        SUM(pid.patientBalance) AS patientBalance
    FROM PatientInvoiceDetails AS pid
    JOIN PatientInvoice AS pi
        ON pi.id = pid.patientInvoiceId
    GROUP BY pi.id
)
UPDATE pi
SET pi.statusId = CASE
        WHEN ar.patientBalance <= 0 THEN 4
        WHEN ar.adjustmentPR > 0 AND ar.patientPayments > 0 AND ar.patientBalance > 0 THEN 3
        ELSE 2
    END
FROM PatientInvoice AS pi
JOIN AggregatedRecords AS ar ON pi.id = ar.id;


COMMIT TRANSACTION;


namespace ClaimLifecycleMonitoring.Domain.Enums;

/// <summary>
/// Represents the ordered stages a healthcare claim moves through during its lifecycle.
/// The numeric values are intentionally sparse to allow additional intermediate stages
/// to be introduced without disturbing persisted data.
/// </summary>
public enum ClaimStage
{
    /// <summary>An appointment has been created for the patient.</summary>
    AppointmentCreated = 10,

    /// <summary>Insurance eligibility verification is being performed.</summary>
    EligibilityVerification = 20,

    /// <summary>Prior authorization is being obtained from the payer.</summary>
    Authorization = 30,

    /// <summary>A claim record has been created in the billing system.</summary>
    ClaimCreated = 40,

    /// <summary>The claim is being validated against business and payer rules.</summary>
    ClaimValidation = 50,

    /// <summary>The 837 EDI transaction has been generated.</summary>
    EdiGenerated837 = 60,

    /// <summary>The 837 EDI transaction has been submitted to the clearinghouse or payer.</summary>
    EdiSubmitted837 = 70,

    /// <summary>A 999 functional acknowledgement has been received.</summary>
    Received999 = 80,

    /// <summary>A 277CA claim acknowledgement has been received.</summary>
    Received277CA = 90,

    /// <summary>The payer is adjudicating the claim.</summary>
    PayerProcessing = 100,

    /// <summary>An 835 remittance advice has been received.</summary>
    Received835 = 110,

    /// <summary>Payment has been posted to the account.</summary>
    PaymentPosting = 120,

    /// <summary>A patient invoice has been generated for any remaining responsibility.</summary>
    PatientInvoice = 130,

    /// <summary>The claim lifecycle is complete.</summary>
    Completed = 140
}

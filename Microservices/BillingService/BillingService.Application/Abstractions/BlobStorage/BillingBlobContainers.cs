namespace BillingService.Application.Abstractions.BlobStorage;

/// <summary>
/// Well-known blob container names for the BillingService domain.
/// All artifact categories have dedicated containers to support
/// independent lifecycle management and access control policies.
/// </summary>
public static class BillingBlobContainers
{
    /// <summary>Outbound EDI 837 claim submission files.</summary>
    public const string ClaimSubmissions = "billing-claim-submissions";

    /// <summary>Inbound clearinghouse 835 ERA payment response files.</summary>
    public const string EraResponses = "billing-era-responses";

    /// <summary>Inbound clearinghouse 999/277 acknowledgement files.</summary>
    public const string Acknowledgements = "billing-acknowledgements";

    /// <summary>270/271 eligibility request and response files.</summary>
    public const string EligibilityFiles = "billing-eligibility";

    /// <summary>Patient invoice PDF artifacts.</summary>
    public const string PatientInvoices = "billing-patient-invoices";

    /// <summary>Claim attachment documents.</summary>
    public const string ClaimAttachments = "billing-claim-attachments";

    /// <summary>Payment remittance attachment documents.</summary>
    public const string PaymentAttachments = "billing-payment-attachments";

    /// <summary>Bulk export and reporting artifacts.</summary>
    public const string Reports = "billing-reports";

    /// <summary>Audit log snapshots and compliance archives.</summary>
    public const string AuditArchives = "billing-audit-archives";
}

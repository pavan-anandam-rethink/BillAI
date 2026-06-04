namespace BillingService.Application.Abstractions.Blob;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>
    /// Default container used for EDI and clearinghouse artifacts when no container is specified.
    /// </summary>
    public string DefaultContainer { get; init; } = "billing-artifacts";

    /// <summary>
    /// Container for EDI request/response files.
    /// </summary>
    public string EdiContainer { get; init; } = "billing-edi";

    /// <summary>
    /// Container for patient invoice exports and PDF artifacts.
    /// </summary>
    public string InvoiceContainer { get; init; } = "billing-invoices";

    /// <summary>
    /// Container for ERA/835 payment response files.
    /// </summary>
    public string EraContainer { get; init; } = "billing-era";
}

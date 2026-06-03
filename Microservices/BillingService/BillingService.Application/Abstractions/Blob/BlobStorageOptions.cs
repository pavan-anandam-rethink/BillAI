namespace BillingService.Application.Abstractions.Blob;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BillingService:BlobStorage";

    /// <summary>Default container for EDI request files.</summary>
    public string EdiRequestContainer { get; init; } = "rtafiles";

    /// <summary>Default container for EDI response/acknowledgement files.</summary>
    public string EdiResponseContainer { get; init; } = "rtafiles";

    /// <summary>Default container for ERA/835 payment response files.</summary>
    public string EraResponseContainer { get; init; } = "rtafiles";

    /// <summary>Default container for eligibility request/response files.</summary>
    public string EligibilityContainer { get; init; } = "rtafiles";

    /// <summary>Default container for exported reports and artifacts.</summary>
    public string ExportsContainer { get; init; } = "rtafiles";
}

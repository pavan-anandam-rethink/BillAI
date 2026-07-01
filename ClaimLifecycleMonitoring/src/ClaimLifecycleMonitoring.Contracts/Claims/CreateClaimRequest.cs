namespace ClaimLifecycleMonitoring.Contracts.Claims;

/// <summary>
/// Request payload for creating a new claim under monitoring.
/// </summary>
public sealed class CreateClaimRequest
{
    /// <summary>Unique claim number.</summary>
    public string ClaimNumber { get; set; } = string.Empty;

    /// <summary>Customer (tenant) code.</summary>
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>Customer display name.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Account (payer) code.</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>Account display name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Patient identifier.</summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Patient display name.</summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>Provider identifier.</summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>Provider display name.</summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>Total billed amount for the claim.</summary>
    public decimal BilledAmount { get; set; }
}

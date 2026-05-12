using ClearingHouse.SharedKernel.Domain.Entities;

namespace ClearingHouse.EdiProcessing.Domain.Entities;

/// <summary>
/// Represents a claim transaction extracted from an EDI file.
/// </summary>
public class ClaimTransaction : BaseEntity
{
    /// <summary>Gets the identifier of the parent EDI file.</summary>
    public Guid EdiFileId { get; private set; }

    /// <summary>Gets the claim identifier.</summary>
    public string ClaimId { get; private set; } = string.Empty;

    /// <summary>Gets the patient control number.</summary>
    public string PatientControlNumber { get; private set; } = string.Empty;

    /// <summary>Gets the total charge amount for the claim.</summary>
    public decimal TotalChargeAmount { get; private set; }

    /// <summary>Gets the date of service, if available.</summary>
    public DateTime? ServiceDate { get; private set; }

    /// <summary>Gets the provider NPI, if available.</summary>
    public string? ProviderNpi { get; private set; }

    /// <summary>Gets the payer identifier, if available.</summary>
    public string? PayerIdentifier { get; private set; }

    /// <summary>Gets the status of the claim transaction.</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="ClaimTransaction"/> instance.
    /// </summary>
    /// <param name="ediFileId">The parent EDI file identifier.</param>
    /// <param name="claimId">The claim identifier.</param>
    /// <param name="patientControlNumber">The patient control number.</param>
    /// <param name="totalChargeAmount">The total charge amount.</param>
    /// <param name="status">The claim status.</param>
    /// <param name="serviceDate">The date of service.</param>
    /// <param name="providerNpi">The provider NPI.</param>
    /// <param name="payerIdentifier">The payer identifier.</param>
    /// <returns>A new <see cref="ClaimTransaction"/> instance.</returns>
    public static ClaimTransaction Create(
        Guid ediFileId,
        string claimId,
        string patientControlNumber,
        decimal totalChargeAmount,
        string status,
        DateTime? serviceDate = null,
        string? providerNpi = null,
        string? payerIdentifier = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claimId);
        ArgumentException.ThrowIfNullOrWhiteSpace(patientControlNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        return new ClaimTransaction
        {
            Id = Guid.NewGuid(),
            EdiFileId = ediFileId,
            ClaimId = claimId,
            PatientControlNumber = patientControlNumber,
            TotalChargeAmount = totalChargeAmount,
            Status = status,
            ServiceDate = serviceDate,
            ProviderNpi = providerNpi,
            PayerIdentifier = payerIdentifier,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private ClaimTransaction() { }
}

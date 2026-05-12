namespace ClearingHouse.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Enum-like value object representing EDI transaction types used in healthcare clearinghouse processing.
/// </summary>
public sealed record EdiTransactionType
{
    /// <summary>
    /// Gets the transaction type code (e.g., "837", "835").
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable description of the transaction type.
    /// </summary>
    public string Description { get; }

    private EdiTransactionType(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>EDI 837 - Health Care Claim submission.</summary>
    public static readonly EdiTransactionType Edi837 = new("837", "Health Care Claim");

    /// <summary>EDI 835 - Health Care Claim Payment/Remittance Advice.</summary>
    public static readonly EdiTransactionType Edi835 = new("835", "Health Care Claim Payment/Remittance Advice");

    /// <summary>EDI 999 - Implementation Acknowledgment.</summary>
    public static readonly EdiTransactionType Edi999 = new("999", "Implementation Acknowledgment");

    /// <summary>EDI 277 - Health Care Claim Status Response.</summary>
    public static readonly EdiTransactionType Edi277 = new("277", "Health Care Claim Status Response");

    /// <summary>EDI 270 - Health Care Eligibility/Benefit Inquiry.</summary>
    public static readonly EdiTransactionType Edi270 = new("270", "Health Care Eligibility/Benefit Inquiry");

    /// <summary>EDI 271 - Health Care Eligibility/Benefit Response.</summary>
    public static readonly EdiTransactionType Edi271 = new("271", "Health Care Eligibility/Benefit Response");

    /// <summary>
    /// Gets all supported EDI transaction types.
    /// </summary>
    public static IReadOnlyCollection<EdiTransactionType> All =>
    [
        Edi837, Edi835, Edi999, Edi277, Edi270, Edi271
    ];

    /// <summary>
    /// Parses a transaction type code string to its corresponding value object.
    /// </summary>
    /// <param name="code">The EDI transaction type code.</param>
    /// <returns>The matching <see cref="EdiTransactionType"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the code is not a recognized EDI type.</exception>
    public static EdiTransactionType FromCode(string code) =>
        All.FirstOrDefault(t => t.Code == code)
        ?? throw new ArgumentException($"Unknown EDI transaction type code: {code}", nameof(code));

    /// <inheritdoc />
    public override string ToString() => $"EDI-{Code}";
}

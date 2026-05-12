namespace ClearingHouse.EdiProcessing.Domain.ValueObjects;

/// <summary>
/// Represents an EDI segment identifier (e.g., ISA, GS, CLM).
/// </summary>
public sealed record EdiSegmentIdentifier
{
    /// <summary>Gets the segment identifier value.</summary>
    public string Value { get; }

    private EdiSegmentIdentifier(string value)
    {
        Value = value;
    }

    /// <summary>Creates an <see cref="EdiSegmentIdentifier"/> from a string value.</summary>
    /// <param name="value">The segment identifier string.</param>
    /// <returns>A new <see cref="EdiSegmentIdentifier"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is null or whitespace.</exception>
    public static EdiSegmentIdentifier FromValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new EdiSegmentIdentifier(value.Trim().ToUpperInvariant());
    }

    /// <summary>Interchange Control Header segment.</summary>
    public static readonly EdiSegmentIdentifier ISA = new("ISA");

    /// <summary>Functional Group Header segment.</summary>
    public static readonly EdiSegmentIdentifier GS = new("GS");

    /// <summary>Functional Group Trailer segment.</summary>
    public static readonly EdiSegmentIdentifier GE = new("GE");

    /// <summary>Interchange Control Trailer segment.</summary>
    public static readonly EdiSegmentIdentifier IEA = new("IEA");

    /// <summary>Transaction Set Header segment.</summary>
    public static readonly EdiSegmentIdentifier ST = new("ST");

    /// <summary>Transaction Set Trailer segment.</summary>
    public static readonly EdiSegmentIdentifier SE = new("SE");

    /// <summary>Claim Information segment.</summary>
    public static readonly EdiSegmentIdentifier CLM = new("CLM");

    /// <summary>Professional Service segment.</summary>
    public static readonly EdiSegmentIdentifier SV1 = new("SV1");

    /// <summary>Institutional Service segment.</summary>
    public static readonly EdiSegmentIdentifier SV2 = new("SV2");

    /// <summary>Subscriber Information segment.</summary>
    public static readonly EdiSegmentIdentifier SBR = new("SBR");

    /// <summary>Health Care Information Codes segment.</summary>
    public static readonly EdiSegmentIdentifier HI = new("HI");

    /// <summary>Individual or Organizational Name segment.</summary>
    public static readonly EdiSegmentIdentifier NM1 = new("NM1");

    /// <summary>Address Information segment.</summary>
    public static readonly EdiSegmentIdentifier N3 = new("N3");

    /// <summary>Geographic Location segment.</summary>
    public static readonly EdiSegmentIdentifier N4 = new("N4");

    /// <summary>Reference Information segment.</summary>
    public static readonly EdiSegmentIdentifier REF = new("REF");

    /// <summary>Date/Time Reference segment.</summary>
    public static readonly EdiSegmentIdentifier DTP = new("DTP");

    /// <summary>Monetary Amount Information segment.</summary>
    public static readonly EdiSegmentIdentifier AMT = new("AMT");

    /// <summary>Claims Adjustment segment.</summary>
    public static readonly EdiSegmentIdentifier CAS = new("CAS");

    /// <summary>Claim Level Payment segment.</summary>
    public static readonly EdiSegmentIdentifier CLP = new("CLP");

    /// <summary>Service Payment Information segment.</summary>
    public static readonly EdiSegmentIdentifier SVC = new("SVC");

    /// <summary>Beginning of Payment Order/Remittance Advice segment.</summary>
    public static readonly EdiSegmentIdentifier BPR = new("BPR");

    /// <summary>Reassociation Trace Number segment.</summary>
    public static readonly EdiSegmentIdentifier TRN = new("TRN");

    /// <summary>Date/Time segment.</summary>
    public static readonly EdiSegmentIdentifier DTM = new("DTM");

    /// <summary>Status Information segment.</summary>
    public static readonly EdiSegmentIdentifier STC = new("STC");

    /// <summary>Beginning of Hierarchical Transaction segment.</summary>
    public static readonly EdiSegmentIdentifier BHT = new("BHT");

    /// <summary>Hierarchical Level segment.</summary>
    public static readonly EdiSegmentIdentifier HL = new("HL");

    /// <summary>Transaction Set Line Number segment.</summary>
    public static readonly EdiSegmentIdentifier LX = new("LX");

    /// <inheritdoc />
    public override string ToString() => Value;
}

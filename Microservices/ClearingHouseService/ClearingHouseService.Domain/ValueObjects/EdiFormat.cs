namespace ClearingHouseService.Domain.ValueObjects
{
    /// <summary>
    /// Represents an EDI format type used in healthcare claim processing.
    /// </summary>
    public sealed class EdiFormat : IEquatable<EdiFormat>
    {
        public static readonly EdiFormat Edi837P = new("837P", "Professional Claim");
        public static readonly EdiFormat Edi837I = new("837I", "Institutional Claim");
        public static readonly EdiFormat Edi837D = new("837D", "Dental Claim");
        public static readonly EdiFormat Edi270 = new("270", "Eligibility Inquiry");
        public static readonly EdiFormat Edi271 = new("271", "Eligibility Response");
        public static readonly EdiFormat Edi835 = new("835", "Payment/Remittance Advice");
        public static readonly EdiFormat Edi999 = new("999", "Functional Acknowledgment");
        public static readonly EdiFormat Edi277 = new("277", "Claim Status Response");
        public static readonly EdiFormat Edi276 = new("276", "Claim Status Request");

        public string Code { get; }
        public string Description { get; }

        private EdiFormat(string code, string description)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>
        /// Creates an EdiFormat from a code string.
        /// </summary>
        public static EdiFormat FromCode(string code)
        {
            return code switch
            {
                "837P" => Edi837P,
                "837I" => Edi837I,
                "837D" => Edi837D,
                "270" => Edi270,
                "271" => Edi271,
                "835" => Edi835,
                "999" => Edi999,
                "277" => Edi277,
                "276" => Edi276,
                _ => throw new ArgumentException($"Unknown EDI format code: {code}", nameof(code))
            };
        }

        public bool Equals(EdiFormat? other) => other is not null && Code == other.Code;
        public override bool Equals(object? obj) => obj is EdiFormat other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => $"{Code} - {Description}";

        public static bool operator ==(EdiFormat? left, EdiFormat? right) => Equals(left, right);
        public static bool operator !=(EdiFormat? left, EdiFormat? right) => !Equals(left, right);
    }
}

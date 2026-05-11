namespace ClearingHouseService.Domain.ValueObjects
{
    /// <summary>
    /// Represents the type of EDI transaction being processed.
    /// </summary>
    public sealed class TransactionType : IEquatable<TransactionType>
    {
        public static readonly TransactionType ClaimSubmission = new("ClaimSubmission", "Submit a claim to a clearing house");
        public static readonly TransactionType EligibilityInquiry = new("EligibilityInquiry", "270/271 eligibility check");
        public static readonly TransactionType RemittanceAdvice = new("RemittanceAdvice", "835 payment/remittance processing");
        public static readonly TransactionType ClaimStatusInquiry = new("ClaimStatusInquiry", "276/277 claim status check");
        public static readonly TransactionType FunctionalAcknowledgment = new("FunctionalAcknowledgment", "999 acknowledgment processing");

        public string Name { get; }
        public string Description { get; }

        private TransactionType(string name, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public static TransactionType FromName(string name)
        {
            return name switch
            {
                "ClaimSubmission" => ClaimSubmission,
                "EligibilityInquiry" => EligibilityInquiry,
                "RemittanceAdvice" => RemittanceAdvice,
                "ClaimStatusInquiry" => ClaimStatusInquiry,
                "FunctionalAcknowledgment" => FunctionalAcknowledgment,
                _ => throw new ArgumentException($"Unknown transaction type: {name}", nameof(name))
            };
        }

        public bool Equals(TransactionType? other) => other is not null && Name == other.Name;
        public override bool Equals(object? obj) => obj is TransactionType other && Equals(other);
        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(TransactionType? left, TransactionType? right) => Equals(left, right);
        public static bool operator !=(TransactionType? left, TransactionType? right) => !Equals(left, right);
    }
}

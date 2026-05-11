namespace ClearingHouseService.Domain.ValueObjects
{
    /// <summary>
    /// Represents a clearing house provider type.
    /// </summary>
    public sealed class ClearingHouseType : IEquatable<ClearingHouseType>
    {
        public static readonly ClearingHouseType Stedi = new("Stedi", TransportProtocol.Sftp);
        public static readonly ClearingHouseType Availity = new("Availity", TransportProtocol.Sftp);

        public string Name { get; }
        public TransportProtocol Protocol { get; }

        private ClearingHouseType(string name, TransportProtocol protocol)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Protocol = protocol;
        }

        public static ClearingHouseType FromName(string name)
        {
            return name switch
            {
                "Stedi" => Stedi,
                "Availity" => Availity,
                _ => throw new ArgumentException($"Unknown clearing house type: {name}", nameof(name))
            };
        }

        public bool Equals(ClearingHouseType? other) => other is not null && Name == other.Name;
        public override bool Equals(object? obj) => obj is ClearingHouseType other && Equals(other);
        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;

        public static bool operator ==(ClearingHouseType? left, ClearingHouseType? right) => Equals(left, right);
        public static bool operator !=(ClearingHouseType? left, ClearingHouseType? right) => !Equals(left, right);
    }

    /// <summary>
    /// Transport protocol used to communicate with a clearing house.
    /// </summary>
    public enum TransportProtocol
    {
        Sftp,
        Api,
        As2
    }
}

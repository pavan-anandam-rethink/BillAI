namespace ClearingHouse.SharedKernel.Domain.ValueObjects;

/// <summary>
/// The connection type supported by a clearinghouse.
/// </summary>
public enum ConnectionType
{
    /// <summary>SFTP-based file transfer.</summary>
    Sftp,

    /// <summary>API-based integration.</summary>
    Api,

    /// <summary>Supports both SFTP and API.</summary>
    Hybrid
}

/// <summary>
/// Strongly typed identifier for a clearinghouse with name, code, and connection type.
/// </summary>
public sealed record ClearinghouseIdentifier
{
    /// <summary>
    /// Gets the display name of the clearinghouse.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique code identifier for the clearinghouse.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the connection type supported by this clearinghouse.
    /// </summary>
    public ConnectionType Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearinghouseIdentifier"/> record.
    /// </summary>
    public ClearinghouseIdentifier(string name, string code, ConnectionType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Clearinghouse name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Clearinghouse code is required.", nameof(code));

        Name = name;
        Code = code;
        Type = type;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Code})";
}

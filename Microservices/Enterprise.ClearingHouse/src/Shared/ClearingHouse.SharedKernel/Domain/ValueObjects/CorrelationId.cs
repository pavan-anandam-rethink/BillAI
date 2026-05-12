namespace ClearingHouse.SharedKernel.Domain.ValueObjects;

/// <summary>
/// Strongly typed correlation identifier for distributed tracing across services.
/// </summary>
public sealed record CorrelationId
{
    /// <summary>
    /// Gets the string value of the correlation ID.
    /// </summary>
    public string Value { get; }

    private CorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Correlation ID cannot be null or empty.", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new correlation ID with a generated GUID value.
    /// </summary>
    public static CorrelationId New() => new(Guid.NewGuid().ToString("D"));

    /// <summary>
    /// Creates a correlation ID from an existing string value.
    /// </summary>
    /// <param name="value">The correlation ID string value.</param>
    public static CorrelationId From(string value) => new(value);

    /// <summary>
    /// Implicitly converts a CorrelationId to its string representation.
    /// </summary>
    public static implicit operator string(CorrelationId correlationId) => correlationId.Value;

    /// <summary>
    /// Implicitly converts a string to a CorrelationId.
    /// </summary>
    public static implicit operator CorrelationId(string value) => From(value);

    /// <inheritdoc />
    public override string ToString() => Value;
}

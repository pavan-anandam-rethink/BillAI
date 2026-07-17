namespace BillAI.RulesEngine.Shared.Guards;

/// <summary>
/// Provides guard clauses to validate method preconditions.
/// </summary>
public static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.</summary>
    public static T NotNull<T>(T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is null or whitespace.</summary>
    public static string NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is less than or equal to zero.</summary>
    public static T Positive<T>(T value, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(default!) <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive.");
        return value;
    }
}

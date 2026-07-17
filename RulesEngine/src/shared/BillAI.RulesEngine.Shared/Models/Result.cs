namespace BillAI.RulesEngine.Shared.Models;

/// <summary>
/// Represents the outcome of an operation, capturing success/failure and optional error details.
/// </summary>
public class Result
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess { get; protected init; }

    /// <summary>Gets whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets a collection of error messages; empty when the operation succeeded.</summary>
    public IReadOnlyList<string> Errors { get; protected init; } = [];

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>Creates a failure result with one or more error messages.</summary>
    public static Result Failure(params string[] errors)
        => new() { IsSuccess = false, Errors = errors };

    /// <summary>Creates a typed successful result.</summary>
    public static Result<T> Success<T>(T value)
        => new() { IsSuccess = true, Value = value };

    /// <summary>Creates a typed failure result.</summary>
    public static Result<T> Failure<T>(params string[] errors)
        => new() { IsSuccess = false, Errors = errors };
}

/// <summary>
/// Represents the outcome of an operation that returns a typed value.
/// </summary>
/// <typeparam name="T">The value type returned on success.</typeparam>
public sealed class Result<T> : Result
{
    private T? _value;

    /// <summary>Gets the value returned on success.</summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing the value of a failed result.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access the value of a failed result.");
            return _value!;
        }
        init => _value = value;
    }
}

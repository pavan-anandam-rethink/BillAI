namespace BillingService.SharedKernel;

public sealed record Result
{
    private Result(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, string.IsNullOrWhiteSpace(error) ? "Operation failed." : error);
}

public sealed record Result<T>
{
    private Result(bool succeeded, T? value, string? error)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
    }

    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, string.IsNullOrWhiteSpace(error) ? "Operation failed." : error);
}

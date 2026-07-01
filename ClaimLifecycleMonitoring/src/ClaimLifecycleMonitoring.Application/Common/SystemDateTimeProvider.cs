using ClaimLifecycleMonitoring.Application.Abstractions.Time;

namespace ClaimLifecycleMonitoring.Application.Common;

/// <summary>Default <see cref="IDateTimeProvider"/> that returns <see cref="DateTime.UtcNow"/>.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}

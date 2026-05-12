namespace ClearingHouse.SftpIngestion.Domain.ValueObjects;

/// <summary>
/// Value object representing a polling schedule with cron expression and execution metadata.
/// </summary>
/// <param name="CronExpression">The cron expression defining the polling schedule.</param>
/// <param name="NextExecutionAt">The next scheduled execution time in UTC.</param>
/// <param name="IsEnabled">Whether this polling schedule is active.</param>
/// <param name="MaxConcurrentPolls">The maximum number of concurrent polls allowed.</param>
public sealed record PollingSchedule(
    string CronExpression,
    DateTime? NextExecutionAt,
    bool IsEnabled,
    int MaxConcurrentPolls)
{
    /// <summary>
    /// Creates a new polling schedule with validation.
    /// </summary>
    /// <param name="cronExpression">The cron expression (e.g., "*/5 * * * *").</param>
    /// <param name="maxConcurrentPolls">Maximum concurrent polls allowed.</param>
    /// <returns>A new <see cref="PollingSchedule"/> instance.</returns>
    public static PollingSchedule Create(string cronExpression, int maxConcurrentPolls = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrentPolls);

        return new PollingSchedule(
            CronExpression: cronExpression,
            NextExecutionAt: null,
            IsEnabled: true,
            MaxConcurrentPolls: maxConcurrentPolls);
    }

    /// <summary>
    /// Creates a disabled polling schedule.
    /// </summary>
    /// <param name="cronExpression">The cron expression.</param>
    /// <returns>A disabled <see cref="PollingSchedule"/> instance.</returns>
    public static PollingSchedule CreateDisabled(string cronExpression)
    {
        return new PollingSchedule(
            CronExpression: cronExpression,
            NextExecutionAt: null,
            IsEnabled: false,
            MaxConcurrentPolls: 1);
    }

    /// <summary>
    /// Returns a new schedule with the next execution time updated.
    /// </summary>
    /// <param name="nextExecution">The next scheduled execution time.</param>
    /// <returns>An updated <see cref="PollingSchedule"/> instance.</returns>
    public PollingSchedule WithNextExecution(DateTime nextExecution)
    {
        return this with { NextExecutionAt = nextExecution };
    }
}

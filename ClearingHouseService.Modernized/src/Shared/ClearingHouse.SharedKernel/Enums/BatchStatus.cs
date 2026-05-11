namespace ClearingHouse.SharedKernel.Enums;

public enum BatchStatus
{
    Created = 0,
    Queued = 1,
    InProgress = 2,
    Completed = 3,
    PartiallyCompleted = 4,
    Failed = 5,
    Cancelled = 6,
    TimedOut = 7
}

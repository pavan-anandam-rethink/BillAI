namespace ClearingHouse.SharedKernel.Resilience;

public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

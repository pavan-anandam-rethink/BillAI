namespace ClearinghousePlugins.Abstractions;

public interface IClearinghouseHealthCheck
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}

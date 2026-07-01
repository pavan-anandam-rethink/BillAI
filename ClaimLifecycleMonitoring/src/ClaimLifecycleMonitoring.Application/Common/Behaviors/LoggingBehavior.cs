using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ClaimLifecycleMonitoring.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behaviour that emits structured logs around every request,
/// including latency and success / failure information.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>Creates a new <see cref="LoggingBehavior{TRequest, TResponse}"/>.</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Handling {RequestName}", requestName);
        try
        {
            var response = await next().ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("Handled {RequestName} in {ElapsedMs} ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failure handling {RequestName} after {ElapsedMs} ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

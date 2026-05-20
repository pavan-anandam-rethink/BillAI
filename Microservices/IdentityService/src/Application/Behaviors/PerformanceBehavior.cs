using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IdentityService.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const int PerformanceThresholdMs = 1000;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > PerformanceThresholdMs)
        {
            logger.LogWarning(
                "Performance threshold exceeded: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, PerformanceThresholdMs);
        }

        return response;
    }
}

using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BillingService.Application.Common.Behaviors;

public sealed class PerformanceLoggingBehavior<TRequest, TResponse>(
    ILogger<PerformanceLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 750;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds >= SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Billing request {RequestName} completed in {ElapsedMilliseconds} ms",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}

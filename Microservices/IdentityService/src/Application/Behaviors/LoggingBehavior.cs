using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IdentityService.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 500)
        {
            logger.LogWarning("Long running request: {RequestName} ({ElapsedMs}ms)", requestName, stopwatch.ElapsedMilliseconds);
        }

        logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
        return response;
    }
}

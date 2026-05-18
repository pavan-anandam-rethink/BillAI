using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BillingService.App.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "CQRS {RequestType} executed in {ElapsedMs} ms",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds);
        }
    }
}


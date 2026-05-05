using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.Web.Observability;

internal sealed class ResilienceDelegatingHandler : DelegatingHandler
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan PerRequestTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan CircuitBreakDuration = TimeSpan.FromSeconds(30);
    private static int _consecutiveFailures;
    private static DateTimeOffset _circuitOpenUntil;

    private readonly ILogger<ResilienceDelegatingHandler> _logger;

    public ResilienceDelegatingHandler(ILogger<ResilienceDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_circuitOpenUntil > DateTimeOffset.UtcNow)
        {
            throw new BrokenCircuitException($"Outbound circuit open until {_circuitOpenUntil:O}");
        }

        var dependency = request.RequestUri?.Host ?? "unknown";
        var method = request.Method.Method;
        var attempt = 0;
        Exception lastException = null;
        HttpResponseMessage lastResponse = null;

        while (attempt <= MaxRetries)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(PerRequestTimeout);

                var response = await base.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
                sw.Stop();

                var transient = IsTransientStatus(response.StatusCode);
                ConcurrencyMetrics.RecordExternalDependency(
                    dependency,
                    method,
                    (int)response.StatusCode,
                    sw.Elapsed.TotalMilliseconds,
                    !transient);

                if (!transient)
                {
                    RecordSuccess();
                    return response;
                }

                response.Dispose();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                lastException = new TimeoutException($"Outbound call timed out after {PerRequestTimeout.TotalSeconds}s");
                ConcurrencyMetrics.RecordExternalDependency(dependency, method, null, sw.Elapsed.TotalMilliseconds, false);
            }
            catch (Exception ex)
            {
                sw.Stop();
                lastException = ex;
                ConcurrencyMetrics.RecordExternalDependency(dependency, method, null, sw.Elapsed.TotalMilliseconds, false);
            }

            attempt++;
            if (attempt > MaxRetries)
            {
                break;
            }

            await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
            request = await CloneHttpRequestMessageAsync(request).ConfigureAwait(false);
        }

        var failures = Interlocked.Increment(ref _consecutiveFailures);
        if (failures >= 5)
        {
            _circuitOpenUntil = DateTimeOffset.UtcNow.Add(CircuitBreakDuration);
            Interlocked.Exchange(ref _consecutiveFailures, 0);
            _logger.LogWarning("Outbound circuit opened for {Seconds}s", CircuitBreakDuration.TotalSeconds);
        }

        if (lastResponse != null)
        {
            return lastResponse;
        }

        throw lastException ?? new HttpRequestException("Outbound dependency call failed.");
    }

    private static bool IsTransientStatus(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static void RecordSuccess()
    {
        Interlocked.Exchange(ref _consecutiveFailures, 0);
        _circuitOpenUntil = DateTimeOffset.MinValue;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace ClearingHouseService.Infrastructure.Resilience
{
    /// <summary>
    /// Provides Polly resilience policies for HTTP and transport operations.
    /// </summary>
    public static class PollyPolicies
    {
        /// <summary>
        /// Creates a retry policy for HTTP requests with exponential backoff.
        /// Retries on transient HTTP errors (5xx) and timeouts.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy(ILogger? logger = null)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger?.LogWarning(
                            "HTTP retry attempt {RetryAttempt} after {Delay}s. Error: {Error}",
                            retryAttempt,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        }

        /// <summary>
        /// Creates a circuit breaker policy for HTTP requests.
        /// Opens the circuit after 5 consecutive failures for 30 seconds.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger? logger = null)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, breakDelay) =>
                    {
                        logger?.LogWarning(
                            "Circuit breaker opened for {BreakDelay}s. Error: {Error}",
                            breakDelay.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    },
                    onReset: () =>
                    {
                        logger?.LogInformation("Circuit breaker reset");
                    });
        }

        /// <summary>
        /// Creates a timeout policy for HTTP requests.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutSeconds = 180)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds));
        }

        /// <summary>
        /// Adds Polly retry and circuit breaker policies to an IHttpClientBuilder.
        /// </summary>
        public static IHttpClientBuilder AddResiliencePolicies(
            this IHttpClientBuilder builder,
            ILogger? logger = null,
            int timeoutSeconds = 180)
        {
            return builder
                .AddPolicyHandler(GetHttpRetryPolicy(logger))
                .AddPolicyHandler(GetCircuitBreakerPolicy(logger));
        }
    }
}

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace ClearingHouse.SharedKernel.Infrastructure.Resilience;

/// <summary>
/// Predefined Polly resilience policies for clearinghouse operations.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates a retry resilience pipeline with exponential backoff.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
    /// <param name="baseDelay">Base delay between retries. Default is 500ms.</param>
    /// <returns>A configured resilience pipeline.</returns>
    public static ResiliencePipeline CreateRetryPipeline(int maxRetries = 3, TimeSpan? baseDelay = null)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                BackoffType = DelayBackoffType.Exponential,
                Delay = baseDelay ?? TimeSpan.FromMilliseconds(500),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<IOException>()
            })
            .Build();
    }

    /// <summary>
    /// Creates a circuit breaker resilience pipeline.
    /// </summary>
    /// <param name="failureThreshold">The failure ratio threshold. Default is 0.5 (50%).</param>
    /// <param name="samplingDuration">The sampling duration. Default is 30 seconds.</param>
    /// <param name="breakDuration">The duration of the break. Default is 60 seconds.</param>
    /// <param name="minimumThroughput">The minimum number of actions in the sampling duration. Default is 10.</param>
    /// <returns>A configured resilience pipeline.</returns>
    public static ResiliencePipeline CreateCircuitBreakerPipeline(
        double failureThreshold = 0.5,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null,
        int minimumThroughput = 10)
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureThreshold,
                SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30),
                BreakDuration = breakDuration ?? TimeSpan.FromSeconds(60),
                MinimumThroughput = minimumThroughput,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
            })
            .Build();
    }

    /// <summary>
    /// Creates a timeout resilience pipeline.
    /// </summary>
    /// <param name="timeout">The timeout duration. Default is 30 seconds.</param>
    /// <returns>A configured resilience pipeline.</returns>
    public static ResiliencePipeline CreateTimeoutPipeline(TimeSpan? timeout = null)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            })
            .Build();
    }

    /// <summary>
    /// Creates a combined resilience pipeline with retry, circuit breaker, and timeout.
    /// </summary>
    /// <returns>A configured combined resilience pipeline.</returns>
    public static ResiliencePipeline CreateCombinedPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(60)
            })
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(500),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<IOException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(60),
                MinimumThroughput = 10,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
            })
            .Build();
    }
}

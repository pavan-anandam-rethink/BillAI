namespace BillingService.Web.Options
{
    public sealed class BillingModernizationOptions
    {
        public const string SectionName = "BillingModernization";
        public const string DefaultCorrelationHeaderName = "X-Correlation-ID";

        public string ServiceName { get; set; } = "BillingService";
        public string ServiceVersion { get; set; } = "1.0.0";
        public CorrelationOptions Correlation { get; set; } = new();
        public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
        public RateLimitingOptions RateLimiting { get; set; } = new();
        public ResponseCompressionOptions ResponseCompression { get; set; } = new();
        public PerformanceOptions Performance { get; set; } = new();

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                ServiceName = "BillingService";
            }

            if (string.IsNullOrWhiteSpace(ServiceVersion))
            {
                ServiceVersion = "1.0.0";
            }

            Correlation ??= new CorrelationOptions();
            OpenTelemetry ??= new OpenTelemetryOptions();
            RateLimiting ??= new RateLimitingOptions();
            ResponseCompression ??= new ResponseCompressionOptions();
            Performance ??= new PerformanceOptions();

            Correlation.Normalize();
            RateLimiting.Normalize();
            Performance.Normalize();
        }
    }

    public sealed class CorrelationOptions
    {
        public bool Enabled { get; set; } = true;
        public string HeaderName { get; set; } = BillingModernizationOptions.DefaultCorrelationHeaderName;

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(HeaderName))
            {
                HeaderName = BillingModernizationOptions.DefaultCorrelationHeaderName;
            }
        }
    }

    public sealed class OpenTelemetryOptions
    {
        public bool Enabled { get; set; } = true;
        public string? OtlpEndpoint { get; set; }
    }

    public sealed class RateLimitingOptions
    {
        public bool Enabled { get; set; }
        public int PermitLimit { get; set; } = 300;
        public int QueueLimit { get; set; }
        public int WindowSeconds { get; set; } = 60;

        public void Normalize()
        {
            if (PermitLimit <= 0)
            {
                PermitLimit = 300;
            }

            if (QueueLimit < 0)
            {
                QueueLimit = 0;
            }

            if (WindowSeconds <= 0)
            {
                WindowSeconds = 60;
            }
        }
    }

    public sealed class ResponseCompressionOptions
    {
        public bool Enabled { get; set; }
    }

    public sealed class PerformanceOptions
    {
        public int SlowRequestThresholdMs { get; set; } = 1000;

        public void Normalize()
        {
            if (SlowRequestThresholdMs <= 0)
            {
                SlowRequestThresholdMs = 1000;
            }
        }
    }
}

using BillingService.App.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BillingService.App.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    private const string CorrelationHeader = "X-Correlation-Id";

    public static IServiceCollection AddBillingObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["Telemetry:ServiceName"] ?? "billingservice-app";
        var otlpEndpoint = configuration["Telemetry:OtlpEndpoint"];

        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddRuntimeInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var accessor = context.RequestServices.GetRequiredService<ICorrelationContextAccessor>();
            var correlationId = context.Request.Headers.TryGetValue(CorrelationHeader, out var incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("N");

            accessor.CorrelationId = correlationId;
            context.Response.Headers[CorrelationHeader] = correlationId;
            await next(context).ConfigureAwait(false);
        });

        return app;
    }
}


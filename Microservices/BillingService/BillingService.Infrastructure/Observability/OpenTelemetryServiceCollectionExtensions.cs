using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BillingService.Infrastructure.Observability;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddBillingOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var telemetryOptions = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder =>
                resourceBuilder.AddService(telemetryOptions.ServiceName, serviceVersion: telemetryOptions.ServiceVersion))
            .WithTracing(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                if (!string.IsNullOrWhiteSpace(telemetryOptions.OtlpEndpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(telemetryOptions.OtlpEndpoint);
                    });
                }
            });

        return services;
    }
}

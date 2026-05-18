using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BillingService.Infrastructure.Observability;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddBillingOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));

        services.AddOpenTelemetry()
            .ConfigureResource((serviceProvider, resourceBuilder) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
                resourceBuilder.AddService(options.ServiceName, serviceVersion: options.ServiceVersion);
            })
            .WithTracing(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = false;
                    });

                var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>();
                if (!string.IsNullOrWhiteSpace(options?.OtlpEndpoint))
                {
                    builder.AddOtlpExporter();
                }
            });

        return services;
    }
}

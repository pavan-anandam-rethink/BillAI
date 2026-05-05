using Authentication.Middlewares;
using AutoMapper;
using BillingService.Domain.Utils;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Domain.Interfaces;

namespace ClearingHouseService.Web
{
    public class Program
    {
        private static IKeyVaultProviderService keyVaultProviderService;
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }
        private static string GetHealthCheckEndpoint(string fullApiUrl, string healthcheckEndpoint = "/healthcheck/live")
        {
            Uri uri = new Uri(fullApiUrl);
            return "https://" + uri.Host + healthcheckEndpoint;
        }
        // Pseudocode plan:
        // 1. Build KeyVaultProviderService to fetch secrets from Azure Key Vault using Azure.Security.KeyVault.Secrets.
        // 2. Register KeyVaultProviderService as a singleton in DI (already present).
        // 3. In Program.cs, fetch required secrets (e.g., billingApiUrl) from KeyVaultProviderService before configuring health checks.
        // 4. Use the fetched secret value in AddHealthChecks().

        // Implementation:

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                        config
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", true, true)
                            .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                            .AddEnvironmentVariables();
                    })
                    .ConfigureServices((hostContext, services) =>
                    {

                        var mapperConfig = new MapperConfiguration(cfg =>
                        {
                            cfg.AddProfile(new MapperProfile());
                        });

                        IMapper mapper = mapperConfig.CreateMapper();

                        services.AddSingleton(mapper);
                        services.AddApplicationInsightsTelemetry(options =>
                        {
                            options.ConnectionString = hostContext.Configuration["ApplicationInsights:APPLICATIONINSIGHTS_CONNECTION_STRING"];
                        });

                        services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();
                        services.AddControllers();
                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "ClearingHouseService.Web", Version = "v1" });
                            c.AddSecurityDefinition("XApiKey", new OpenApiSecurityScheme()
                            {
                                Name = "XApiKey",
                                Type = SecuritySchemeType.ApiKey,
                                Scheme = "ApiKeyScheme",
                                In = ParameterLocation.Header,
                                Description = "API Key header. \r\n\r\n Enter your key in the text input below.\r\n\r\nExample: \"1safsfsdfdfd\"",
                            });
                            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                            {
                               new OpenApiSecurityScheme {
                                   Reference = new OpenApiReference {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "XApiKey"
                                        }
                                    },
                               new string[] {}
                                }
                            });
                        });

                        // Build a temporary provider to resolve KeyVaultProviderService
                        using var provider = services.BuildServiceProvider();
                        keyVaultProviderService = provider.GetRequiredService<IKeyVaultProviderService>();
                        // Fetch the billingApiUrl secret from Key Vault
                        var billingApiUrl = keyVaultProviderService.GetSecretAsync(hostContext.Configuration.GetSection("BillingApiUrl").Value).Result;

                        services.AddHealthChecks()
                                .AddUrlGroup([new Uri(GetHealthCheckEndpoint(billingApiUrl, "/api/health"))]);

                    })
                    .Configure((hostContext, app) =>
                    {
                        app.UseHttpsRedirection();
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseMiddleware<ApiKeyMiddleware>();
                        app.UseHealthChecks("/api/health", new HealthCheckOptions()
                        {
                            Predicate = _ => true,
                            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails
                        });
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClearingHouseService.Web v1");
                        });
                    })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();

                    var appInsightsSecretName = context.Configuration["ApplicationInsights:APPLICATIONINSIGHTS_CONNECTION_STRING"];
                    var appInsightsConnectionString = keyVaultProviderService.GetSecretAsync(appInsightsSecretName).Result;

                    if (string.IsNullOrWhiteSpace(appInsightsConnectionString))
                    {
                        throw new InvalidOperationException(
                            $"Application Insights connection string not found in Key Vault for secret '{appInsightsSecretName}'");
                    }

                    logging.AddApplicationInsights(
                        configureTelemetryConfiguration: tc =>
                        {
                            tc.ConnectionString = appInsightsConnectionString;
                        },
                        configureApplicationInsightsLoggerOptions: options => { });

                    logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Information);
                    logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);

                    LogConfigHelper.ConfigureWorkerLogging(logging);
                    new ServicesConfiguration(logging.Services, context.Configuration, logging, keyVaultProviderService).Configure();

                    logging.AddConsole();

                });
                });
    }
}



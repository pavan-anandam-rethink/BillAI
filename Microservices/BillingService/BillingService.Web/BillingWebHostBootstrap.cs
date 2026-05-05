using System;
using System.Threading.Tasks;
using Billing.FolderStructure.Core.Services;
using BillingService.Web.IoC;
using BillingService.Web.Servers;
using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Common.Logging.Extensions;

namespace BillingService.Web;

/// <summary>Async composition for <see cref="Startup"/>; one synchronous bridge at host configure time.</summary>
internal static class BillingWebHostBootstrap
{
    internal const string ApiKeyHeader = "XApiKey";

    public static async Task ConfigureServicesAsync(
        IServiceCollection services,
        IConfiguration configuration,
        IKeyVaultProviderService kv)
    {
        var billingConnTask = IoCContainer.GetDBConnectionStringAsync(configuration, "Database", kv);
        var reportingConnTask = IoCContainer.GetDBConnectionStringAsync(configuration, "ReportingDB", kv);
        await Task.WhenAll(billingConnTask, reportingConnTask).ConfigureAwait(false);

        IoCContainer.ConfigureDatabase(services, billingConnTask.Result, reportingConnTask.Result);
        IoCContainer.RegisterDBContext(services);

        var httpKeysTask = IoCContainer.ResolveClientHttpKeysAsync(configuration, kv);
        var redisConnTask = kv.GetSecretAsync(configuration["RedisCache:ConnectionString"]);
        var svcBusConnTask = kv.GetSecretAsync(configuration["ConnectionStrings:ServiceBus:ConnectionString"]);
        var blobConnTask = kv.GetSecretAsync(configuration["ConnectionStrings:BlobStorage:ConnectionString"]);
        var aiInstTask = kv.GetSecretAsync(configuration["ApplicationInsights:InstrumentationKey"]);
        var aiConnStrTask = kv.GetSecretAsync(configuration["ApplicationInsights:APPLICATIONINSIGHTS_CONNECTION_STRING"]);
        var pusherAppIdTask = kv.GetSecretAsync(configuration["Pusher:AppId"]);
        var pusherKeyTask = kv.GetSecretAsync(configuration["Pusher:Key"]);
        var pusherSecretTask = kv.GetSecretAsync(configuration["Pusher:Secret"]);

        await Task.WhenAll(
            httpKeysTask,
            redisConnTask,
            svcBusConnTask,
            blobConnTask,
            aiInstTask,
            aiConnStrTask,
            pusherAppIdTask,
            pusherKeyTask,
            pusherSecretTask).ConfigureAwait(false);

        await IoCContainer.RegisterServicesAsync(
            services,
            configuration,
            kv,
            blobConnTask.Result,
            svcBusConnTask.Result).ConfigureAwait(false);

        IoCContainer.RegisterRedisCache(services, redisConnTask.Result);
        IoCContainer.RegisterHttpClients(services, configuration, httpKeysTask.Result);

        services.AddSingleton<IPusherNotificationServer>(_ =>
            new PusherNotificationServer(
                configuration,
                pusherAppIdTask.Result,
                pusherKeyTask.Result,
                pusherSecretTask.Result));

        var aiStarter = new ApplicationInsightsStarter();
        aiStarter.TryAddIfConfigured(aiConnStrTask.Result, services);

        services.AddRethinkLogging(configuration);

        services.AddControllers();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Billing Service API",
                Version = "v1"
            });
            c.AddSecurityDefinition("XApiKey", new OpenApiSecurityScheme
            {
                Name = ApiKeyHeader,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme",
                In = ParameterLocation.Header,
                Description = "API Key header. \r\n\r\n Enter your key in the text input below.\r\n\r\nExample: \"1safsfsdfdfd\"",
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme.",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "XApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddHealthChecks()
            .AddSqlServer(billingConnTask.Result, name: "SQL Server")
            .AddAzureServiceBusQueue(svcBusConnTask.Result, Queues.RT_Billing_ClearingHouse_ClaimSubmission, name: "Service Bus")
            .AddAzureBlobStorage(
                _ => new BlobServiceClient(blobConnTask.Result),
                _ => new AzureBlobStorageHealthCheckOptions { ContainerName = "rtafiles" },
                name: "Blob Storage")
            .AddAzureApplicationInsights(aiInstTask.Result, name: "App Insights")
            .AddUrlGroup(
            [
                new Uri(GetHealthCheckEndpoint(configuration["AccountsApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["CurriculumApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["DemographicsApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["HealthPlansApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["HealthInsuranceApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["MedicalRecordsApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["PracticeOperationsApiUrl"])),
                new Uri(GetHealthCheckEndpoint(configuration["AppointmentApiUrl"], "/api/cal/healthcheck/live")),
            ], name: "Rethink Microservices");
    }

    private static string GetHealthCheckEndpoint(string fullApiUrl, string healthcheckEndpoint = "/healthcheck/live")
    {
        var uri = new Uri(fullApiUrl);
        return "https://" + uri.Host + healthcheckEndpoint;
    }
}

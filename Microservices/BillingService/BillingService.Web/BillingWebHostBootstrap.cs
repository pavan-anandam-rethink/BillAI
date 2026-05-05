using System;
using System.Threading.Tasks;
using Billing.FolderStructure.Core.Services;
using BillingService.Web.IoC;
using BillingService.Web.Observability;
using BillingService.Web.Servers;
using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Domain.Interfaces;
using RethinkCore.Common.Logging.Extensions;
using HealthChecks.UI.Client;
using Authentication.Middlewares;

namespace BillingService.Web;

/// <summary>Async composition for minimal hosting — avoids synchronous blocking during service registration.</summary>
public static class BillingWebHostBootstrap
{
    internal const string ApiKeyHeader = "XApiKey";

    public static async Task AddServicesAsync(WebApplicationBuilder builder, IKeyVaultProviderService kv)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;
        services.AddSingleton<DbCommandMetricsListener>();
        services.AddHostedService(sp => sp.GetRequiredService<DbCommandMetricsListener>());

        var billingConnTask = IoCContainer.GetDBConnectionStringAsync(configuration, "Database", kv);
        var reportingConnTask = IoCContainer.GetDBConnectionStringAsync(configuration, "ReportingDB", kv);
        await Task.WhenAll(billingConnTask, reportingConnTask).ConfigureAwait(false);

        var billingConn = await billingConnTask.ConfigureAwait(false);
        var reportingConn = await reportingConnTask.ConfigureAwait(false);

        IoCContainer.ConfigureDatabase(services, billingConn, reportingConn);
        IoCContainer.RegisterDBContext(services);

        var httpKeysTask = IoCContainer.ResolveClientHttpKeysAsync(configuration, kv);
        var redisConnTask = kv.GetSecretAsync(configuration["ConnectionStrings:RedisCache:ConnectionString"]);
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

        var redisConn = await redisConnTask.ConfigureAwait(false);
        var svcBusConn = await svcBusConnTask.ConfigureAwait(false);
        var blobConn = await blobConnTask.ConfigureAwait(false);
        var httpKeys = await httpKeysTask.ConfigureAwait(false);
        var aiInst = await aiInstTask.ConfigureAwait(false);
        var aiConnStr = await aiConnStrTask.ConfigureAwait(false);
        var pusherAppId = await pusherAppIdTask.ConfigureAwait(false);
        var pusherKey = await pusherKeyTask.ConfigureAwait(false);
        var pusherSecret = await pusherSecretTask.ConfigureAwait(false);

        await IoCContainer.RegisterServicesAsync(
            services,
            configuration,
            kv,
            blobConn,
            svcBusConn).ConfigureAwait(false);

        IoCContainer.RegisterRedisCache(services, redisConn);
        IoCContainer.RegisterHttpClients(services, configuration, httpKeys);

        services.AddSingleton<IPusherNotificationServer>(_ =>
            new PusherNotificationServer(
                configuration,
                pusherAppId,
                pusherKey,
                pusherSecret));

        var aiStarter = new ApplicationInsightsStarter();
        aiStarter.TryAddIfConfigured(aiConnStr, services);

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

        services.AddHostedService<DbCommandMetricsHostedService>();

        services.AddHealthChecks()
            .AddSqlServer(billingConn, name: "SQL Server")
            .AddAzureServiceBusQueue(svcBusConn, Queues.RT_Billing_ClearingHouse_ClaimSubmission, name: "Service Bus")
            .AddAzureBlobStorage(
                _ => new BlobServiceClient(blobConn),
                _ => new AzureBlobStorageHealthCheckOptions { ContainerName = "rtafiles" },
                name: "Blob Storage")
            .AddAzureApplicationInsights(aiInst, name: "App Insights")
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

    public static void ConfigurePipeline(WebApplication app)
    {
        var env = app.Environment;
        if (!env.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition"));
        app.UseRouting();
        app.UseMiddleware<RequestTelemetryMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseWhen(
            context => !context.Request.Headers.ContainsKey(ApiKeyHeader),
            branch => branch.UseMiddleware<JwtMiddleware>());
        app.UseWhen(
            context => context.Request.Headers.ContainsKey(ApiKeyHeader),
            branch => branch.UseMiddleware<ApiKeyMiddleware>());

        app.UseSwagger();
        app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing Service"); });

        app.UseHealthChecks("/api/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = env.IsProduction()
                ? UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails
                : UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapControllers();

        try
        {
            // Force initialization so EF Core command listener is subscribed early.
            _ = app.Services.GetRequiredService<DbCommandMetricsListener>();
            var billingBlobService = app.Services.GetRequiredService<IBillingBlobService>();
            _ = billingBlobService.CreateBlobContainerAsync();
        }
        catch { }
    }

    private static string GetHealthCheckEndpoint(string fullApiUrl, string healthcheckEndpoint = "/healthcheck/live")
    {
        var uri = new Uri(fullApiUrl);
        return "https://" + uri.Host + healthcheckEndpoint;
    }
}

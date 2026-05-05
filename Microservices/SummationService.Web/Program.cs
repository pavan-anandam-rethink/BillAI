using Authentication.Middlewares;
using Azure.Identity;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.OpenApi.Models;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Domain.Interfaces;


namespace SummationService.Web
{
    public class Program
    {
        public static IKeyVaultProviderService keyVaultProviderService { get; set; }

        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }
        public static string APIKEY { get; private set; } = "XApiKey";

        //private static string GetHealthCheckEndpoint(string fullApiUrl, string healthcheckEndpoint = "/healthcheck/live")
        //{
        //    Uri uri = new Uri(fullApiUrl);
        //    return "https://" + uri.Host + healthcheckEndpoint;
        //}
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
                        services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();

                        using var provider = services.BuildServiceProvider();
                        keyVaultProviderService = provider.GetRequiredService<IKeyVaultProviderService>();

                        services.AddApplicationInsightsTelemetry(options =>
                        {
                            options.ConnectionString =
                                hostContext.Configuration["ApplicationInsights:ConnectionString"];
                        });
                        services.AddSingleton<IKeyVaultProviderService, KeyVaultProviderService>();
                        services.AddControllers();
                        services.AddHttpClient();
                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "SummationService.Web", Version = "v1" });
                            c.AddSecurityDefinition("XApiKey", new OpenApiSecurityScheme()
                            {
                                Name = "XApiKey",
                                Type = SecuritySchemeType.ApiKey,
                                Scheme = "apikeyscheme",
                                In = ParameterLocation.Header,
                                Description = "api key header. \r\n\r\n enter your key in the text input below.\r\n\r\nexample: \"1safsfsdfdfd\"",
                            });
                            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                            {
                                Name = "Authorization",
                                Type = SecuritySchemeType.ApiKey,
                                Scheme = "Bearer",
                                BearerFormat = "JWT",
                                In = ParameterLocation.Header,
                                Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
                            });
                            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                            {
                                new OpenApiSecurityScheme {
                                    Reference = new OpenApiReference {
                                        Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                    }
                                },
                                new string[] {}
                            }
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
                        //services.AddHealthChecks()
                        //        .AddUrlGroup([new Uri(GetHealthCheckEndpoint(hostContext.Configuration.GetSection("BillingAPIURL").Value, "/api/health"))]);
                        //Skipping SFTP Healthcheck since they are stored in Rethink.
                    })
                    .Configure((hostContext, app) =>
                    {
                        app.UseSwagger();
                        app.UseSwaggerUI(c =>
                        {
                            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SummationService.Web v1");
                        });
                        app.UseHttpsRedirection();
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseWhen(
                            context => !context.Request.Headers.ContainsKey(APIKEY),
                            app => app.UseMiddleware<JwtMiddleware>()
                        );
                        app.UseWhen(
                            context => context.Request.Headers.ContainsKey(APIKEY),
                            app => app.UseMiddleware<ApiKeyMiddleware>()
                        );
                        //app.UseHealthChecks("/api/health", new HealthCheckOptions()
                        //{
                        //    Predicate = _ => true,
                        //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails
                        //});
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    })
                    .ConfigureLogging(async (context, logging) =>
                    {
                        logging.ClearProviders();
                        logging.AddApplicationInsights(
                        configureTelemetryConfiguration: (tc) =>
                        {
                            var appInsightsSecretName =
                                 context.Configuration["ApplicationInsights:ConnectionString"];

                            tc.ConnectionString = keyVaultProviderService.GetSecretAsync(appInsightsSecretName).Result
                             ?? Environment.GetEnvironmentVariable("ConnectionString");
                        },
                        configureApplicationInsightsLoggerOptions: (options) =>
                        {
                        });

                        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Information);
                        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);


                        LogConfigHelper.ConfigureWorkerLogging(logging);
                        await new ServicesConfiguration(logging.Services, context.Configuration, logging).Configure();
                    });
                });
    }
}



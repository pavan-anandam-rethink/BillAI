using Authentication.Middlewares;
using Azure.Storage.Blobs;
using Billing.FolderStructure.Core.Services;
using BillingService.Web.IoC;
using BillingService.Web.Middlewares;
using BillingService.Web.Servers;
using HealthChecks.Azure.Storage.Blobs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Domain.Interfaces;
using System;

namespace BillingService.Web
{
    public class Startup
    {
        public IKeyVaultProviderService KeyVaultProviderService { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            KeyVaultProviderService = new KeyVaultProviderService(Configuration);
        }
        public IConfiguration Configuration { get; }

        public string APIKEY { get; private set; } = "XApiKey";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IKeyVaultProviderService>(_ => KeyVaultProviderService);

            IoCContainer.ConfigureDatabase(services, Configuration, KeyVaultProviderService);
            IoCContainer.RegisterDBContext(services);
            IoCContainer.RegisterServices(services, Configuration, KeyVaultProviderService).Wait();
            IoCContainer.RegisterHttpClientsAsync(services, Configuration, KeyVaultProviderService).GetAwaiter().GetResult();
            IoCContainer.RegisterRedisCacheAsync(services, Configuration, KeyVaultProviderService).GetAwaiter().GetResult();

            services.AddSingleton<IPusherNotificationServer, PusherNotificationServer>();
            services.AddMemoryCache();
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Billing Service API",
                    Version = "v1"
                });
                c.AddSecurityDefinition("XApiKey", new OpenApiSecurityScheme()
                {
                    Name = "XApiKey",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "ApiKeyScheme",
                    In = ParameterLocation.Header,
                    Description = "API Key header. \r\n\r\n Enter your key in the text input below.\r\n\r\nExample: \"1safsfsdfdfd\"",
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


            services.AddHealthChecks()
                .AddSqlServer(IoCContainer.GetDBConnectionString(Configuration, "Database", KeyVaultProviderService), name: "SQL Server")
                .AddAzureServiceBusQueue(KeyVaultProviderService.GetSecretAsync(Configuration["ConnectionStrings:ServiceBus:ConnectionString"]).Result, Queues.RT_Billing_ClearingHouse_ClaimSubmission, name: "Service Bus")
                .AddAzureBlobStorage(clientFactory: sp => new BlobServiceClient(KeyVaultProviderService.GetSecretAsync(Configuration["ConnectionStrings:BlobStorage:ConnectionString"]).Result),
                optionsFactory: sp => new AzureBlobStorageHealthCheckOptions()
                {
                    //Use any container to check if blob storage is working. If required, check all containers one by one
                    ContainerName = "rtafiles"  //"availity","eramanualupload"
                }, name: "Blob Storage")
                .AddAzureApplicationInsights(KeyVaultProviderService.GetSecretAsync(Configuration["ApplicationInsights:InstrumentationKey"]).Result, name: "App Insights")
                .AddUrlGroup([
                    new Uri(GetHealthCheckEndpoint(Configuration["AccountsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["CurriculumApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["DemographicsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["HealthPlansApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["HealthInsuranceApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["MedicalRecordsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["PracticeOperationsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["AppointmentApiUrl"], "/api/cal/healthcheck/live")),
                ], name: "Rethink Microservices");


        }

        private string GetHealthCheckEndpoint(string fullApiUrl, string healthcheckEndpoint = "/healthcheck/live")
        {
            Uri uri = new Uri(fullApiUrl);
            return "https://" + uri.Host + healthcheckEndpoint;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            //app.UseDeveloperExceptionPage();

            app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Content-Disposition"));
            app.UseMiddleware<RequestLatencyLoggingMiddleware>();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseWhen(
                context => !context.Request.Headers.ContainsKey(APIKEY),
                branch =>
                {
                    branch.UseMiddleware<JwtMiddleware>();
                    branch.UseMiddleware<BillingMasterDataRequestMiddleware>();
                });
            app.UseWhen(
                context => context.Request.Headers.ContainsKey(APIKEY),
                app => app.UseMiddleware<ApiKeyMiddleware>()
            );

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing Service");
            });
            app.UseHealthChecks("/api/health", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = env.IsProduction() ? UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails : UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            try
            {
                var billingBlobService = app.ApplicationServices.GetRequiredService<IBillingBlobService>();
                billingBlobService.CreateBlobContainerAsync();
            }
            catch { }
        }
    }
}
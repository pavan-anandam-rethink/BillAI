using Authentication.Middlewares;
using ClientService.Web.IoC;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Rethink.Services.Domain.Interfaces;

namespace ClientService.Web
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
            IoCContainer.ConfigureDatabase(services, Configuration, KeyVaultProviderService);
            IoCContainer.RegisterDBContext(services);
            IoCContainer.RegisterServices(services, Configuration, KeyVaultProviderService).Wait();
            IoCContainer.RegisterHttpClients(services, Configuration, KeyVaultProviderService);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Rethink Data Service API",
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
                        },
                        In = ParameterLocation.Header,
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
                .AddRedis(KeyVaultProviderService.GetSecretAsync(Configuration["ConnectionStrings:RedisCache:ConnectionString"]).Result, name: "Redis Cache")
                .AddAzureApplicationInsights(KeyVaultProviderService.GetSecretAsync(Configuration["ApplicationInsights:InstrumentationKey"]).Result, name: "App Insights")
                .AddUrlGroup([
                    new Uri(GetHealthCheckEndpoint(Configuration["AccountsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["CurriculumApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["DemographicsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["HealthPlansApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["HealthInsuranceApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["MedicalRecordsApiUrl"])),
                    new Uri(GetHealthCheckEndpoint(Configuration["PracticeOperationsApiUrl"])),
                ], name: "Rethink Microservices")
                //.AddAzureKeyVault(new Uri(Configuration["KeyVaultUri"]), new DefaultAzureCredential(),
                //options =>
                //{
                //    //Add key names to check the existence of keys
                //    //foreach (string secret in new string[] { "key1", "key2", "key3" })
                //    //{
                //    //    options.AddSecret(secret);
                //    //}
                //}, name: "Key Vault")
                ;
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
            app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
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

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rethink Data Service");
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
        }
    }
}
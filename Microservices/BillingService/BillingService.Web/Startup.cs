using Authentication.Middlewares;
using Billing.FolderStructure.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using HealthChecks.UI.Client;
using Rethink.Services.Domain.Interfaces;

namespace BillingService.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            KeyVaultProviderService = new KeyVaultProviderService(Configuration);
        }

        public IConfiguration Configuration { get; }

        /// <remarks>Concrete type is registered for tests and direct DI; callers should depend on <see cref="IKeyVaultProviderService"/>.</remarks>
        public KeyVaultProviderService KeyVaultProviderService { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(KeyVaultProviderService);
            services.AddSingleton<IKeyVaultProviderService>(sp => sp.GetRequiredService<KeyVaultProviderService>());

            BillingWebHostBootstrap.ConfigureServicesAsync(services, Configuration, KeyVaultProviderService)
                .GetAwaiter()
                .GetResult();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition"));
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseWhen(
                context => !context.Request.Headers.ContainsKey(BillingWebHostBootstrap.ApiKeyHeader),
                branch => branch.UseMiddleware<JwtMiddleware>());
            app.UseWhen(
                context => context.Request.Headers.ContainsKey(BillingWebHostBootstrap.ApiKeyHeader),
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

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            try
            {
                var billingBlobService = app.ApplicationServices.GetRequiredService<IBillingBlobService>();
                _ = billingBlobService.CreateBlobContainerAsync();
            }
            catch { }
        }
    }
}

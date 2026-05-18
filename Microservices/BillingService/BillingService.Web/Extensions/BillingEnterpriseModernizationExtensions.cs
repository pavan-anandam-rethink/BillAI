using BillingService.Web.Middlewares;
using BillingService.Web.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.RateLimiting;

namespace BillingService.Web.Extensions
{
    public static class BillingEnterpriseModernizationExtensions
    {
        public static IServiceCollection AddBillingEnterpriseModernization(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<BillingModernizationOptions>()
                .Bind(configuration.GetSection(BillingModernizationOptions.SectionName))
                .PostConfigure(options => options.Normalize());

            var options = configuration
                .GetSection(BillingModernizationOptions.SectionName)
                .Get<BillingModernizationOptions>() ?? new BillingModernizationOptions();
            options.Normalize();

            if (options.ResponseCompression.Enabled)
            {
                services.AddResponseCompression();
            }

            if (options.RateLimiting.Enabled)
            {
                services.AddRateLimiter(rateLimiterOptions =>
                {
                    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var accountPartition = context.User?.FindFirst("AccountInfoId")?.Value;
                        var partitionKey = string.IsNullOrWhiteSpace(accountPartition)
                            ? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
                            : accountPartition;

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = options.RateLimiting.PermitLimit,
                                QueueLimit = options.RateLimiting.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                Window = TimeSpan.FromSeconds(options.RateLimiting.WindowSeconds)
                            });
                    });
                });
            }

            return services;
        }

        public static IApplicationBuilder UseBillingEnterpriseModernization(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<BillingModernizationOptions>>().Value;
            options.Normalize();

            if (options.ResponseCompression.Enabled)
            {
                app.UseResponseCompression();
            }

            app.UseMiddleware<CorrelationIdMiddleware>();

            return app;
        }

        public static IApplicationBuilder UseBillingEnterpriseRateLimiting(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<BillingModernizationOptions>>().Value;
            options.Normalize();

            if (options.RateLimiting.Enabled)
            {
                app.UseRateLimiter();
            }

            return app;
        }
    }
}

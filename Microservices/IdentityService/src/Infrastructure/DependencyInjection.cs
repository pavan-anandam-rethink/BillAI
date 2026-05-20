using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Token service
        services.AddTransient<ITokenService, TokenService>();

        // Cache service (Redis)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "IdentityService_";
        });
        services.AddScoped<ICacheService, CacheService>();

        // Token validation API
        var tokenValidationApi = configuration["TokenValidationApi"]!.TrimEnd('/') + "/";
        services.AddHttpClient("tokenValidation", client =>
        {
            client.BaseAddress = new Uri(tokenValidationApi);
            client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("BillingHttp:RequestTimeoutSeconds", 120));
        });
        services.AddScoped<ITokenValidationApiService, TokenValidationApiService>();

        // Current user service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}

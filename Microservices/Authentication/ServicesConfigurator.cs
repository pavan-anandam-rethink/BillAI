using Authentication.Interfaces;
using Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Authentication
{
    [ExcludeFromCodeCoverage]
    public static class ServicesConfigurator
    {
        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.AddTransient<ITokenService, TokenService>();
        }
    }
}

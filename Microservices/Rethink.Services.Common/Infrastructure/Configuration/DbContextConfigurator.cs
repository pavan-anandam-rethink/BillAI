using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rethink.Services.Common.Infrastructure.Context;
using Rethink.Services.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration
{
    [ExcludeFromCodeCoverage]
    public class DbContextConfigurator<T> : IDbContextConfigurator<T>
        where T : BaseDbContext<T>
    {
        public void Configure(IServiceCollection serviceCollection, string connectionString, bool usePooling = false,
            bool registerImmediately = false)
        {
            if (usePooling)
            {
                serviceCollection.AddDbContextPool<T>(
                    opt => opt.UseSqlServer(connectionString, o =>
                    {
                        o.EnableRetryOnFailure();
                    }));
            }
            else
            {
                serviceCollection.AddDbContext<T>(
                    opt => opt.UseSqlServer(connectionString, o =>
                    {
                        o.EnableRetryOnFailure();
                    }));
            }

            if (registerImmediately) Register(serviceCollection);
        }
        public void Register(IServiceCollection services)
        {
            services.AddScoped<T>();
            services.AddScoped<BaseDbContext<T>, T>();
        }
    }
}
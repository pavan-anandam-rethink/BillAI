    using Microsoft.Extensions.DependencyInjection;
using Rethink.Services.Common.Infrastructure.Context;

namespace Rethink.Services.Common.Interfaces
{
    public interface IDbContextConfigurator<T> where T : BaseDbContext<T>
    {
        public void Configure(IServiceCollection serviceCollection, string connectionString, bool usePooling = false,
            bool registerImmediately = false);

        public void Register(IServiceCollection services);
    }
}

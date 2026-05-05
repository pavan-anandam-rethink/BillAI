using BillingService.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.XUnit.Integration.Tests;

public sealed class BillingWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection> _configureTestServices;

    public BillingWebApplicationFactory(Action<IServiceCollection> configureTestServices)
    {
        _configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(IntegrationTestKeyVaultProvider.EnvironmentName);
        builder.ConfigureTestServices(_configureTestServices);
    }
}

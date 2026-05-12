using ClearingHouse.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace ClearingHouse.Plugins.Runtime;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClearinghousePlugins(this IServiceCollection services, Action<PluginRegistrationBuilder> configure)
    {
        services.AddSingleton<ClearinghousePluginFactory>();
        services.AddSingleton<IClearinghousePluginFactory>(sp => sp.GetRequiredService<ClearinghousePluginFactory>());

        var builder = new PluginRegistrationBuilder(services);
        configure(builder);

        return services;
    }
}

public class PluginRegistrationBuilder
{
    private readonly IServiceCollection _services;

    public PluginRegistrationBuilder(IServiceCollection services) => _services = services;

    public PluginRegistrationBuilder AddPlugin<TPlugin, TConfig>(string clearinghouseId, TConfig configuration)
        where TPlugin : class, IClearinghousePlugin
        where TConfig : class
    {
        _services.AddSingleton(configuration);
        _services.AddSingleton<TPlugin>();
        _services.AddSingleton<IClearinghousePlugin>(sp => sp.GetRequiredService<TPlugin>());
        return this;
    }
}

using ClearingHouse.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.Plugins.Runtime;

public class ClearinghousePluginFactory : IClearinghousePluginFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClearinghousePluginFactory> _logger;
    private readonly Dictionary<string, Type> _pluginRegistry = new(StringComparer.OrdinalIgnoreCase);

    public ClearinghousePluginFactory(IServiceProvider serviceProvider, ILogger<ClearinghousePluginFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void RegisterPlugin<TPlugin>(string clearinghouseId) where TPlugin : IClearinghousePlugin
    {
        _pluginRegistry[clearinghouseId] = typeof(TPlugin);
        _logger.LogInformation("Registered plugin {PluginType} for clearinghouse {ClearinghouseId}",
            typeof(TPlugin).Name, clearinghouseId);
    }

    public IClearinghousePlugin GetPlugin(string clearinghouseId)
    {
        if (!_pluginRegistry.TryGetValue(clearinghouseId, out var pluginType))
            throw new InvalidOperationException($"No plugin registered for clearinghouse: {clearinghouseId}");

        var plugin = (IClearinghousePlugin)_serviceProvider.GetRequiredService(pluginType);

        if (!plugin.IsEnabled)
            throw new InvalidOperationException($"Plugin for clearinghouse '{clearinghouseId}' is disabled");

        return plugin;
    }

    public IReadOnlyList<IClearinghousePlugin> GetAllPlugins()
    {
        return _pluginRegistry.Values
            .Select(type => (IClearinghousePlugin)_serviceProvider.GetRequiredService(type))
            .Where(p => p.IsEnabled)
            .ToList();
    }

    public bool IsPluginRegistered(string clearinghouseId) => _pluginRegistry.ContainsKey(clearinghouseId);
}

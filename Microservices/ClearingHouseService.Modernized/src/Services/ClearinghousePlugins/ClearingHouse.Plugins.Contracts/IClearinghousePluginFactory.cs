namespace ClearingHouse.Plugins.Contracts;

public interface IClearinghousePluginFactory
{
    IClearinghousePlugin GetPlugin(string clearinghouseId);
    IReadOnlyList<IClearinghousePlugin> GetAllPlugins();
    bool IsPluginRegistered(string clearinghouseId);
}

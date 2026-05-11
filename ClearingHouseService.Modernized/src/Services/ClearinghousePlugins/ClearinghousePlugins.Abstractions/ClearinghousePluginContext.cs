namespace ClearinghousePlugins.Abstractions;

public class ClearinghousePluginContext
{
    public string CorrelationId { get; init; } = string.Empty;
    public string AccountId { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public IDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}

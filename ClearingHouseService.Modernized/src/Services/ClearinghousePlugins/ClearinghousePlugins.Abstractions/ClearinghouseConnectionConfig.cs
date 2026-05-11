namespace ClearinghousePlugins.Abstractions;

public class ClearinghouseConnectionConfig
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? PrivateKeyPath { get; init; }
    public string UploadDirectory { get; init; } = string.Empty;
    public string DownloadDirectory { get; init; } = string.Empty;
    public string? ApiKey { get; init; }
    public string? ApiBaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public IDictionary<string, string> AdditionalSettings { get; init; } = new Dictionary<string, string>();
}

namespace AiRulesEngine.Web.Options;

public class AiProviderOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1-mini";
    public string ApiKeyHeader { get; set; } = "Authorization";
    public string ApiKeyPrefix { get; set; } = "Bearer";
}

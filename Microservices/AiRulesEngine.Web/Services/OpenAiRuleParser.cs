using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using Microsoft.Extensions.Options;

namespace AiRulesEngine.Web.Services;

public sealed class OpenAiRuleParser : IAiRuleParser
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;
    private readonly RuleEngineOptions _engineOptions;
    private readonly IHostEnvironment _environment;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OpenAiRuleParser(
        HttpClient httpClient,
        IOptions<AiProviderOptions> options,
        IOptions<RuleEngineOptions> engineOptions,
        IHostEnvironment environment)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _engineOptions = engineOptions.Value;
        _environment = environment;
    }

    public async Task<RuleSetDefinition> ParseAsync(string content, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("AI parsing is disabled. Enable AiProvider:Enabled to use this feature.");
        }

        var prompt = await LoadPromptAsync(cancellationToken);
        var requestBody = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "system", content = prompt },
                new { role = "user", content }
            },
            temperature = 0.0
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, _serializerOptions), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var headerValue = string.IsNullOrWhiteSpace(_options.ApiKeyPrefix)
                ? _options.ApiKey
                : $"{_options.ApiKeyPrefix} {_options.ApiKey}";
            request.Headers.TryAddWithoutValidation(_options.ApiKeyHeader, headerValue);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var responseJson = JsonDocument.Parse(responseContent);
        var messageContent = responseJson.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(messageContent))
        {
            throw new InvalidOperationException("AI response did not contain rule JSON.");
        }

        var ruleSet = JsonSerializer.Deserialize<RuleSetDefinition>(messageContent, _serializerOptions);
        if (ruleSet == null)
        {
            throw new InvalidOperationException("Unable to parse AI response into a rule set.");
        }

        return ruleSet;
    }

    private async Task<string> LoadPromptAsync(CancellationToken cancellationToken)
    {
        var promptPath = Path.Combine(_environment.ContentRootPath, _engineOptions.PromptPath);
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Prompt file not found at {promptPath}");
        }

        return await File.ReadAllTextAsync(promptPath, cancellationToken);
    }
}

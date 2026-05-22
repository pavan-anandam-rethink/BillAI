using System.Text.Json;
using System.Text.Json.Serialization;
using AiRulesEngine.Web.Models;
using AiRulesEngine.Web.Options;
using Microsoft.Extensions.Options;

namespace AiRulesEngine.Web.Services;

public sealed class FileRuleSetStore : IRuleSetStore
{
    private readonly RuleEngineOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FileRuleSetStore(IOptions<RuleEngineOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public async Task SaveAsync(RuleSetDefinition ruleSet, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ruleSet.Id))
        {
            ruleSet.Id = Guid.NewGuid().ToString("N");
        }

        Directory.CreateDirectory(GetStoragePath());
        var filePath = GetRuleSetPath(ruleSet.Id);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, ruleSet, _serializerOptions, cancellationToken);
    }

    public async Task<RuleSetDefinition?> GetAsync(string ruleSetId, CancellationToken cancellationToken)
    {
        var filePath = GetRuleSetPath(ruleSetId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<RuleSetDefinition>(stream, _serializerOptions, cancellationToken);
    }

    private string GetStoragePath() => Path.Combine(_environment.ContentRootPath, _options.StoragePath);

    private string GetRuleSetPath(string ruleSetId) => Path.Combine(GetStoragePath(), $"{ruleSetId}.json");
}

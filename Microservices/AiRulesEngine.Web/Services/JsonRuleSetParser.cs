using System.Text.Json;
using System.Text.Json.Serialization;
using AiRulesEngine.Web.Models;

namespace AiRulesEngine.Web.Services;

public sealed class JsonRuleSetParser
{
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public bool TryParse(string content, out RuleSetDefinition ruleSet)
    {
        ruleSet = null!;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<RuleSetDefinition>(content, _serializerOptions);
            if (parsed == null)
            {
                return false;
            }

            ruleSet = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

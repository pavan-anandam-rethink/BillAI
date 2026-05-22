using System.Text.Json;

namespace AiRulesEngine.Web.Models;

public class RuleValidationRequest
{
    public string RuleSetId { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
}

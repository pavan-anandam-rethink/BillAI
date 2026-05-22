namespace AiRulesEngine.Web.Models;

public class WorkflowActionDefinition
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

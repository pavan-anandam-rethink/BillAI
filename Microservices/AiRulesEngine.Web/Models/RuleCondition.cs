namespace AiRulesEngine.Web.Models;

public class RuleCondition
{
    public string Path { get; set; } = string.Empty;
    public RuleOperator Operator { get; set; }
    public string? Value { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string? CompareToPath { get; set; }
}

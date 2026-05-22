using System.Text.Json;
using System.Text.RegularExpressions;
using AiRulesEngine.Web.Models;

namespace AiRulesEngine.Web.Services;

public sealed class JsonRuleEvaluator : IRuleEvaluator
{
    public IReadOnlyList<RuleEvaluationResult> Evaluate(RuleSetDefinition ruleSet, JsonElement payload)
    {
        var results = new List<RuleEvaluationResult>();

        foreach (var rule in ruleSet.Rules)
        {
            if (rule.AppliesWhen.Count > 0 && !EvaluateConditions(rule.AppliesWhen, rule.AppliesWhenLogic, payload))
            {
                continue;
            }

            if (!EvaluateConditions(rule.Conditions, rule.ConditionLogic, payload))
            {
                results.Add(new RuleEvaluationResult
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Severity = rule.Severity,
                    Message = rule.Message
                });
            }
        }

        return results;
    }

    public IReadOnlyList<WorkflowActionResult> EvaluateWorkflows(RuleSetDefinition ruleSet, JsonElement payload)
    {
        var actions = new List<WorkflowActionResult>();

        foreach (var workflow in ruleSet.WorkflowRules)
        {
            if (!EvaluateConditions(workflow.Conditions, workflow.ConditionLogic, payload))
            {
                continue;
            }

            foreach (var action in workflow.Actions)
            {
                actions.Add(new WorkflowActionResult
                {
                    RuleId = workflow.Id,
                    RuleName = workflow.Name,
                    Type = action.Type,
                    Message = action.Message,
                    Metadata = action.Metadata
                });
            }
        }

        return actions;
    }

    private static bool EvaluateConditions(IReadOnlyCollection<RuleCondition> conditions, RuleConditionLogic logic, JsonElement payload)
    {
        if (conditions.Count == 0)
        {
            return true;
        }

        return logic == RuleConditionLogic.All
            ? conditions.All(condition => EvaluateCondition(condition, payload))
            : conditions.Any(condition => EvaluateCondition(condition, payload));
    }

    private static bool EvaluateCondition(RuleCondition condition, JsonElement payload)
    {
        if (!TryResolvePath(payload, condition.Path, out var element))
        {
            return condition.Operator == RuleOperator.NotEquals;
        }

        switch (condition.Operator)
        {
            case RuleOperator.Required:
                return IsPresent(element);
            case RuleOperator.Equals:
                return string.Equals(GetStringValue(element), condition.Value, StringComparison.OrdinalIgnoreCase);
            case RuleOperator.NotEquals:
                return !string.Equals(GetStringValue(element), condition.Value, StringComparison.OrdinalIgnoreCase);
            case RuleOperator.Regex:
                return MatchesRegex(element, condition.Value);
            case RuleOperator.Numeric:
                return IsNumeric(element);
            case RuleOperator.LengthEquals:
                return HasLength(element, condition.Min ?? ParseInt(condition.Value));
            case RuleOperator.LengthBetween:
                return HasLengthBetween(element, condition.Min, condition.Max);
            case RuleOperator.DateWithinDays:
                return IsWithinDays(payload, element, condition);
            default:
                return false;
        }
    }

    private static bool TryResolvePath(JsonElement element, string path, out JsonElement value)
    {
        value = element;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty(segment, out var child))
            {
                value = child;
                continue;
            }

            if (value.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
            {
                if (index >= 0 && index < value.GetArrayLength())
                {
                    value = value[index];
                    continue;
                }
            }

            return false;
        }

        return true;
    }

    private static bool IsPresent(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return !string.IsNullOrWhiteSpace(element.GetString());
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.GetArrayLength() > 0;
        }

        return true;
    }

    private static string? GetStringValue(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : element.ToString();
    }

    private static bool MatchesRegex(JsonElement element, string? pattern)
    {
        var value = GetStringValue(element);
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        return Regex.IsMatch(value, pattern);
    }

    private static bool IsNumeric(JsonElement element)
    {
        var value = GetStringValue(element);
        return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
    }

    private static bool HasLength(JsonElement element, int? length)
    {
        if (length is null)
        {
            return false;
        }

        var value = GetStringValue(element) ?? string.Empty;
        return value.Length == length.Value;
    }

    private static bool HasLengthBetween(JsonElement element, int? min, int? max)
    {
        if (min is null || max is null)
        {
            return false;
        }

        var value = GetStringValue(element) ?? string.Empty;
        return value.Length >= min.Value && value.Length <= max.Value;
    }

    private static bool IsWithinDays(JsonElement payload, JsonElement element, RuleCondition condition)
    {
        if (!DateTime.TryParse(GetStringValue(element), out var dateValue))
        {
            return false;
        }

        if (condition.Max is null || string.IsNullOrWhiteSpace(condition.CompareToPath))
        {
            return false;
        }

        if (!TryResolvePath(payload, condition.CompareToPath, out var compareElement))
        {
            return false;
        }

        if (!DateTime.TryParse(GetStringValue(compareElement), out var compareValue))
        {
            return false;
        }

        var difference = (compareValue.Date - dateValue.Date).TotalDays;
        return difference >= 0 && difference <= condition.Max.Value;
    }

    private static int? ParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}

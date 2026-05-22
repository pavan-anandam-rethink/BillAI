using AiRulesEngine.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiRulesEngine.Web.Services
{
    public static class RuleEvaluator
    {
        public static IReadOnlyList<RuleViolation> Evaluate(RuleSet ruleSet, ClaimRulesEngineContext context)
        {
            if (ruleSet == null || context == null)
            {
                return Array.Empty<RuleViolation>();
            }

            var root = JsonSerializer.SerializeToElement(context, new JsonSerializerOptions { WriteIndented = false });
            var violations = new List<RuleViolation>();

            foreach (var rule in ruleSet.Rules ?? Enumerable.Empty<RuleDefinition>())
            {
                var conditions = rule.Conditions ?? new List<RuleCondition>();
                var conditionResults = conditions.Select(condition => EvaluateCondition(root, condition)).ToList();

                var isValid = rule.MatchMode == RuleMatchMode.Any
                    ? conditionResults.Any(result => result)
                    : conditionResults.All(result => result);

                if (!isValid)
                {
                    violations.Add(new RuleViolation
                    {
                        RuleId = rule.Id,
                        Message = rule.Message ?? rule.Name ?? "Rule violation",
                        Severity = rule.Severity,
                        Metadata = new Dictionary<string, string>
                        {
                            ["RuleName"] = rule.Name ?? string.Empty
                        }
                    });
                }
            }

            return violations;
        }

        private static bool EvaluateCondition(JsonElement root, RuleCondition condition)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.Field))
            {
                return false;
            }

            if (!TryGetValue(root, condition.Field, out var valueElement))
            {
                return condition.Operator == RuleOperator.NotEquals;
            }

            var value = valueElement.ValueKind switch
            {
                JsonValueKind.String => valueElement.GetString(),
                JsonValueKind.Number => valueElement.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => valueElement.GetRawText()
            };

            return condition.Operator switch
            {
                RuleOperator.Required => !string.IsNullOrWhiteSpace(value),
                RuleOperator.Equals => string.Equals(value, condition.Value, StringComparison.OrdinalIgnoreCase),
                RuleOperator.NotEquals => !string.Equals(value, condition.Value, StringComparison.OrdinalIgnoreCase),
                RuleOperator.GreaterThan => CompareNumeric(value, condition.Value) > 0,
                RuleOperator.LessThan => CompareNumeric(value, condition.Value) < 0,
                RuleOperator.Regex => !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, condition.Value ?? string.Empty),
                RuleOperator.Numeric => !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit),
                RuleOperator.LengthEquals => !string.IsNullOrWhiteSpace(value) && value.Length == ParseInt(condition.Value),
                RuleOperator.LengthMin => !string.IsNullOrWhiteSpace(value) && value.Length >= ParseInt(condition.Value),
                RuleOperator.LengthMax => !string.IsNullOrWhiteSpace(value) && value.Length <= ParseInt(condition.Value),
                RuleOperator.In => condition.Values.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        private static bool TryGetValue(JsonElement element, string path, out JsonElement value)
        {
            value = element;
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out var next))
                {
                    value = default;
                    return false;
                }

                value = next;
            }

            return true;
        }

        private static int CompareNumeric(string actualValue, string expectedValue)
        {
            var actual = ParseDouble(actualValue);
            var expected = ParseDouble(expectedValue);
            return actual.CompareTo(expected);
        }

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, out var parsed) ? parsed : 0;
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out var parsed) ? parsed : 0;
        }
    }
}

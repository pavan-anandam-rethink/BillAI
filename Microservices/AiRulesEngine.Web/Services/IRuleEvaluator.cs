using System.Text.Json;
using AiRulesEngine.Web.Models;

namespace AiRulesEngine.Web.Services;

public interface IRuleEvaluator
{
    IReadOnlyList<RuleEvaluationResult> Evaluate(RuleSetDefinition ruleSet, JsonElement payload);
    IReadOnlyList<WorkflowActionResult> EvaluateWorkflows(RuleSetDefinition ruleSet, JsonElement payload);
}

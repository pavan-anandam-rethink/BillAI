using System.Text.Json;
using BillAI.RulesEngine.Application.Interfaces.Services;
using BillAI.RulesEngine.Domain.Entities.Rules;
using BillAI.RulesEngine.Domain.Enums;
using BillAI.RulesEngine.Engine.Evaluators;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BillAI.RulesEngine.Engine.Tests;

public sealed class RulesEvaluationEngineTests
{
    private static RulesEvaluationEngine CreateEngine()
        => new(NullLogger<RulesEvaluationEngine>.Instance);

    private static RuleDefinition CreatePublishedRule(
        string name,
        string expression,
        RuleSeverity severity = RuleSeverity.Error,
        int priority = 100)
    {
        var rule = RuleDefinition.Create("tenant-1", name, "Claims", expression, "system", severity: severity, priority: priority);
        rule.Publish("system");
        return rule;
    }

    [Fact]
    public async Task EvaluateAsync_AllRulesPass_ShouldReturnIsValidTrue()
    {
        var expression = JsonSerializer.Serialize(new
        {
            field = "claim.amount",
            @operator = "GreaterThan",
            value = 0,
            dataType = "Numeric"
        });

        var rule = CreatePublishedRule("Amount > 0", expression);
        var engine = CreateEngine();

        var claim = JsonSerializer.Deserialize<JsonElement>("{\"claim\": {\"amount\": 100}}");
        var result = await engine.EvaluateAsync([rule], claim!);

        Assert.True(result.IsValid);
        Assert.Single(result.PassedRules);
        Assert.Empty(result.FailedRules);
    }

    [Fact]
    public async Task EvaluateAsync_RuleFails_ShouldReturnIsValidFalse()
    {
        var expression = JsonSerializer.Serialize(new
        {
            field = "claim.amount",
            @operator = "GreaterThan",
            value = 1000,
            dataType = "Numeric"
        });

        var rule = CreatePublishedRule("Amount > 1000", expression);
        var engine = CreateEngine();

        var claim = JsonSerializer.Deserialize<JsonElement>("{\"claim\": {\"amount\": 50}}");
        var result = await engine.EvaluateAsync([rule], claim!);

        Assert.False(result.IsValid);
        Assert.Empty(result.PassedRules);
        Assert.Single(result.FailedRules);
    }

    [Fact]
    public async Task EvaluateAsync_NoRules_ShouldReturnIsValidTrue()
    {
        var engine = CreateEngine();
        var claim = JsonSerializer.Deserialize<JsonElement>("{\"claim\": {\"amount\": 50}}");
        var result = await engine.EvaluateAsync([], claim!);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EvaluateSingleAsync_PassingRule_ShouldReturnPassed()
    {
        var expression = JsonSerializer.Serialize(new
        {
            field = "patientId",
            @operator = "IsNotNull",
            dataType = "String"
        });

        var rule = CreatePublishedRule("PatientId Required", expression);
        var engine = CreateEngine();

        var data = JsonSerializer.Deserialize<JsonElement>("{\"patientId\": \"P-001\"}");
        var result = await engine.EvaluateSingleAsync(rule, data!);

        Assert.True(result.Passed);
        Assert.Equal("PatientId Required", result.RuleName);
    }

    [Fact]
    public async Task EvaluateAsync_RulesSortedByPriority()
    {
        var expressionTrue = JsonSerializer.Serialize(new
        {
            field = "value",
            @operator = "Equals",
            value = "test",
            dataType = "String"
        });

        var rule1 = CreatePublishedRule("Priority 200 Rule", expressionTrue, priority: 200);
        var rule2 = CreatePublishedRule("Priority 50 Rule", expressionTrue, priority: 50);

        var engine = CreateEngine();
        var data = JsonSerializer.Deserialize<JsonElement>("{\"value\": \"test\"}");
        var result = await engine.EvaluateAsync([rule1, rule2], data!);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.PassedRules.Count);
        // Priority 50 should appear first
        Assert.Equal("Priority 50 Rule", result.PassedRules[0].RuleName);
    }

    [Fact]
    public async Task EvaluateAsync_InvalidExpression_ShouldMarkAsFailed()
    {
        var rule = CreatePublishedRule("Bad Rule", "not-valid-json");
        var engine = CreateEngine();

        var data = JsonSerializer.Deserialize<JsonElement>("{\"value\": \"test\"}");
        var result = await engine.EvaluateAsync([rule], data!);

        Assert.False(result.IsValid);
        Assert.Single(result.FailedRules);
    }
}

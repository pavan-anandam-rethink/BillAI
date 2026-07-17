using BillAI.RulesEngine.Engine.Evaluators;
using BillAI.RulesEngine.Engine.Operators;
using BillAI.RulesEngine.Domain.Enums;
using System.Text.Json;
using Xunit;

namespace BillAI.RulesEngine.Engine.Tests;

public sealed class ConditionEvaluatorTests
{
    [Theory]
    [InlineData("hello", "hello", ConditionOperator.Equals, DataType.String, true)]
    [InlineData("hello", "world", ConditionOperator.Equals, DataType.String, false)]
    [InlineData("hello", "HELLO", ConditionOperator.Equals, DataType.String, true)] // case insensitive
    [InlineData(10, 5, ConditionOperator.GreaterThan, DataType.Numeric, true)]
    [InlineData(3, 5, ConditionOperator.GreaterThan, DataType.Numeric, false)]
    [InlineData(5, 5, ConditionOperator.GreaterThanOrEqual, DataType.Numeric, true)]
    [InlineData(100, 50, ConditionOperator.LessThan, DataType.Numeric, false)]
    [InlineData(null, null, ConditionOperator.IsNull, DataType.String, true)]
    [InlineData("value", null, ConditionOperator.IsNotNull, DataType.String, true)]
    public void Evaluate_VariousConditions_ShouldReturnExpectedResult(
        object? left, object? right, ConditionOperator op, DataType dataType, bool expected)
    {
        var result = ConditionEvaluator.Evaluate(left, op, right, dataType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_Contains_ShouldBeCaseInsensitive()
    {
        var result = ConditionEvaluator.Evaluate("Hello World", ConditionOperator.Contains, "WORLD", DataType.String);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NotContains_ShouldReturnFalseWhenContained()
    {
        var result = ConditionEvaluator.Evaluate("Hello World", ConditionOperator.NotContains, "World", DataType.String);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_StartsWith_ShouldWork()
    {
        var result = ConditionEvaluator.Evaluate("HCFA-1500", ConditionOperator.StartsWith, "HCFA", DataType.String);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_Regex_ShouldMatchPattern()
    {
        var result = ConditionEvaluator.Evaluate("ABC-12345", ConditionOperator.Regex, @"^[A-Z]{3}-\d{5}$", DataType.String);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_Regex_ShouldReturnFalseForNoMatch()
    {
        var result = ConditionEvaluator.Evaluate("invalid", ConditionOperator.Regex, @"^\d+$", DataType.String);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_In_ShouldMatchAnyValue()
    {
        var result = ConditionEvaluator.Evaluate("B", ConditionOperator.In, "A,B,C", DataType.String);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NotIn_ShouldReturnTrueWhenNotInList()
    {
        var result = ConditionEvaluator.Evaluate("D", ConditionOperator.NotIn, "A,B,C", DataType.String);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_Between_Numeric_ShouldWork()
    {
        var result = ConditionEvaluator.Evaluate(50, ConditionOperator.Between, "10,100", DataType.Numeric);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_Between_Numeric_OutsideRange_ShouldReturnFalse()
    {
        var result = ConditionEvaluator.Evaluate(5, ConditionOperator.Between, "10,100", DataType.Numeric);
        Assert.False(result);
    }
}

public sealed class FieldExtractorTests
{
    [Fact]
    public void Extract_TopLevelField_ShouldReturnValue()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("{\"claimId\": \"CL-001\"}");
        var value = FieldExtractor.Extract(json, "claimId");
        Assert.Equal("CL-001", value);
    }

    [Fact]
    public void Extract_NestedField_ShouldReturnValue()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("{\"patient\": {\"age\": 35}}");
        var value = FieldExtractor.Extract(json, "patient.age");
        Assert.Equal(35m, Convert.ToDecimal(value));
    }

    [Fact]
    public void Extract_MissingField_ShouldReturnNull()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("{\"claimId\": \"CL-001\"}");
        var value = FieldExtractor.Extract(json, "nonExistentField");
        Assert.Null(value);
    }

    [Fact]
    public void Extract_ArrayIndex_ShouldReturnElement()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("{\"items\": [\"A\", \"B\", \"C\"]}");
        var value = FieldExtractor.Extract(json, "items[1]");
        Assert.Equal("B", value);
    }

    [Fact]
    public void Extract_ArrayIndexOutOfBounds_ShouldReturnNull()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("{\"items\": [\"A\"]}");
        var value = FieldExtractor.Extract(json, "items[5]");
        Assert.Null(value);
    }
}

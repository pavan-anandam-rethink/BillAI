using System.Text.Json;
using BillAI.RulesEngine.Domain.Enums;

namespace BillAI.RulesEngine.Engine.Operators;

/// <summary>
/// Provides stateless comparison operations for rule condition evaluation.
/// All methods are thread-safe.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>Evaluates a condition operator against left and right values.</summary>
    public static bool Evaluate(
        object? leftValue,
        ConditionOperator op,
        object? rightValue,
        DataType dataType)
    {
        return op switch
        {
            ConditionOperator.IsNull => leftValue is null,
            ConditionOperator.IsNotNull => leftValue is not null,
            ConditionOperator.Equals => AreEqual(leftValue, rightValue, dataType),
            ConditionOperator.NotEquals => !AreEqual(leftValue, rightValue, dataType),
            ConditionOperator.GreaterThan => Compare(leftValue, rightValue, dataType) > 0,
            ConditionOperator.GreaterThanOrEqual => Compare(leftValue, rightValue, dataType) >= 0,
            ConditionOperator.LessThan => Compare(leftValue, rightValue, dataType) < 0,
            ConditionOperator.LessThanOrEqual => Compare(leftValue, rightValue, dataType) <= 0,
            ConditionOperator.Contains => StringContains(leftValue, rightValue, contains: true),
            ConditionOperator.NotContains => StringContains(leftValue, rightValue, contains: false),
            ConditionOperator.StartsWith => StringStartsWith(leftValue, rightValue),
            ConditionOperator.EndsWith => StringEndsWith(leftValue, rightValue),
            ConditionOperator.In => InList(leftValue, rightValue),
            ConditionOperator.NotIn => !InList(leftValue, rightValue),
            ConditionOperator.Regex => RegexMatch(leftValue, rightValue),
            ConditionOperator.Between => BetweenValues(leftValue, rightValue, dataType),
            _ => false
        };
    }

    private static bool AreEqual(object? left, object? right, DataType dataType)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;

        return dataType switch
        {
            DataType.Numeric => ToDecimal(left) == ToDecimal(right),
            DataType.Boolean => ToBoolean(left) == ToBoolean(right),
            DataType.Date or DataType.DateTime => ToDateTimeOffset(left) == ToDateTimeOffset(right),
            _ => string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase)
        };
    }

    private static int Compare(object? left, object? right, DataType dataType)
    {
        if (left is null || right is null)
            return left is null ? -1 : 1;

        return dataType switch
        {
            DataType.Numeric => ToDecimal(left).CompareTo(ToDecimal(right)),
            DataType.Date or DataType.DateTime => ToDateTimeOffset(left).CompareTo(ToDateTimeOffset(right)),
            _ => string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool StringContains(object? left, object? right, bool contains)
    {
        var leftStr = left?.ToString() ?? string.Empty;
        var rightStr = right?.ToString() ?? string.Empty;
        var result = leftStr.Contains(rightStr, StringComparison.OrdinalIgnoreCase);
        return contains ? result : !result;
    }

    private static bool StringStartsWith(object? left, object? right)
        => (left?.ToString() ?? string.Empty).StartsWith(right?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static bool StringEndsWith(object? left, object? right)
        => (left?.ToString() ?? string.Empty).EndsWith(right?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static bool InList(object? left, object? right)
    {
        if (right is null) return false;

        IEnumerable<object> list = right switch
        {
            IEnumerable<object> collection => collection,
            JsonElement { ValueKind: JsonValueKind.Array } je => je.EnumerateArray().Select(e => (object)e.GetRawText()),
            _ => right.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => (object)s.Trim())
        };

        var leftStr = left?.ToString() ?? string.Empty;
        return list.Any(item => string.Equals(item?.ToString(), leftStr, StringComparison.OrdinalIgnoreCase));
    }

    private static bool RegexMatch(object? left, object? right)
    {
        if (left is null || right is null) return false;
        var pattern = right.ToString()!;
        var input = left.ToString()!;
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, pattern);
        }
        catch
        {
            return false;
        }
    }

    private static bool BetweenValues(object? left, object? right, DataType dataType)
    {
        // right must be a comma-separated "min,max"
        if (left is null || right is null) return false;
        var parts = right.ToString()!.Split(',');
        if (parts.Length != 2) return false;

        if (dataType == DataType.Numeric)
        {
            var val = ToDecimal(left);
            return val >= ToDecimal(parts[0]) && val <= ToDecimal(parts[1]);
        }

        if (dataType is DataType.Date or DataType.DateTime)
        {
            var dt = ToDateTimeOffset(left);
            return dt >= ToDateTimeOffset(parts[0]) && dt <= ToDateTimeOffset(parts[1]);
        }

        return false;
    }

    private static decimal ToDecimal(object? value)
    {
        if (value is JsonElement je)
            return je.ValueKind == JsonValueKind.Number ? je.GetDecimal() : decimal.Parse(je.GetString() ?? "0");
        return Convert.ToDecimal(value);
    }

    private static bool ToBoolean(object? value)
    {
        if (value is JsonElement je)
            return je.ValueKind == JsonValueKind.True;
        return Convert.ToBoolean(value);
    }

    private static DateTimeOffset ToDateTimeOffset(object? value)
    {
        if (value is JsonElement je)
            return DateTimeOffset.Parse(je.GetString() ?? DateTimeOffset.MinValue.ToString());
        return DateTimeOffset.Parse(value?.ToString() ?? DateTimeOffset.MinValue.ToString());
    }
}

using System.Text.Json;

namespace BillAI.RulesEngine.Engine.Evaluators;

/// <summary>
/// Extracts field values from a JSON data context using dot-notation paths.
/// Thread-safe and stateless.
/// </summary>
public static class FieldExtractor
{
    /// <summary>
    /// Extracts a value from <paramref name="dataContext"/> using the dot-notation <paramref name="fieldPath"/>.
    /// Returns null when the path does not exist.
    /// </summary>
    public static object? Extract(object dataContext, string fieldPath)
    {
        var element = ToJsonElement(dataContext);
        if (element is null) return null;

        var segments = fieldPath.Split('.');
        JsonElement current = element.Value;

        foreach (var segment in segments)
        {
            // Support array indexing: items[0]
            if (segment.Contains('[') && segment.EndsWith(']'))
            {
                var bracketIndex = segment.IndexOf('[');
                var propertyName = segment[..bracketIndex];
                var indexStr = segment[(bracketIndex + 1)..^1];

                if (!current.TryGetProperty(propertyName, out var arrayElement))
                    return null;

                if (arrayElement.ValueKind != JsonValueKind.Array)
                    return null;

                if (!int.TryParse(indexStr, out var arrayIndex))
                    return null;

                var items = arrayElement.EnumerateArray().ToList();
                if (arrayIndex < 0 || arrayIndex >= items.Count)
                    return null;

                current = items[arrayIndex];
            }
            else
            {
                if (!current.TryGetProperty(segment, out var next))
                    return null;
                current = next;
            }
        }

        return ExtractPrimitive(current);
    }

    private static JsonElement? ToJsonElement(object dataContext)
    {
        if (dataContext is JsonElement je) return je;
        if (dataContext is string json)
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch
            {
                return null;
            }
        }
        // Serialise POCO and back
        var serialised = JsonSerializer.Serialize(dataContext);
        return JsonSerializer.Deserialize<JsonElement>(serialised);
    }

    private static object? ExtractPrimitive(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetDecimal(out var d) ? d : (object?)element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Undefined => null,
        _ => element // Return the whole element for arrays/objects
    };
}

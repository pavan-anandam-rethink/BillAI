using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Helpers;

public static class EntityComparer
{
    public static (Dictionary<string, object> OldValues, Dictionary<string, object> NewValues)
    GetChanges<T>(this T oldEntity, T newEntity, IEnumerable<string>? ignoreFields = null)
    {
        var ignoreSet = ignoreFields is null
            ? EmptyIgnoreSet
            : new HashSet<string>(ignoreFields, StringComparer.OrdinalIgnoreCase);

        var oldValues = new Dictionary<string, object>();
        var newValues = new Dictionary<string, object>();

        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (ignoreSet.Contains(prop.Name))
                continue;

            var oldValue = prop.GetValue(oldEntity);
            var newValue = prop.GetValue(newEntity);

            if (!Equals(oldValue, newValue))
            {
                oldValues[prop.Name] = oldValue;
                newValues[prop.Name] = newValue;
            }
        }

        return (oldValues, newValues);
    }


    public static Dictionary<string, object> GetChanges<T>(this T newEntity, IEnumerable<string>? ignoreFields = null)
    {
        var ignoreSet = ignoreFields is null
            ? EmptyIgnoreSet
            : new HashSet<string>(ignoreFields, StringComparer.OrdinalIgnoreCase);


        var currentValues = new Dictionary<string, object>();

        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            if (ignoreSet.Contains(prop.Name))
                continue;

            var newValue = prop.GetValue(newEntity);
            currentValues[prop.Name] = newValue;
        }

        return currentValues;
    }

    private static readonly HashSet<string> EmptyIgnoreSet =
    new(StringComparer.OrdinalIgnoreCase);
}
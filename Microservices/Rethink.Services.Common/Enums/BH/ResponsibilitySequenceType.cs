using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.Serialization;
using System.Reflection;

namespace Rethink.Services.Common.Enums.BH
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResponsibilitySequenceType
    {
        [EnumMember(Value = "P")]
        Primary = 'P',
        [EnumMember(Value = "S")]
        Secondary = 'S',
        [EnumMember(Value = "T")]
        Tertiary = 'T',
        [EnumMember(Value = "4")]
        Four = '4',
        [EnumMember(Value = "5")]
        Five = '5',
        [EnumMember(Value = "6")]
        Six = '6',
        [EnumMember(Value = "7")]
        Seven = '7',
        [EnumMember(Value = "8")]
        Eight = '8',
        [EnumMember(Value = "9")]
        Nine = '9'
    }

    public static class ResponsibilitySequenceTypeHelper
    {
        public static string AsString(this ResponsibilitySequenceType responsibilitySequence)
        {
            return ((char)responsibilitySequence).ToString();
        }
        public static ResponsibilitySequenceType FromString(string sequenceTypeStr)
        {
            return (ResponsibilitySequenceType)sequenceTypeStr[0];
        }

        public static int AsOrdinal(this ResponsibilitySequenceType responsibilitySequence)
        {
            switch (responsibilitySequence)
            {
                case ResponsibilitySequenceType.Primary: return 1;
                case ResponsibilitySequenceType.Secondary: return 2;
                case ResponsibilitySequenceType.Tertiary: return 3;
                default: return int.Parse(responsibilitySequence.AsString());
            }
        }

        public static ResponsibilitySequenceType FromOrdinal(int responsibilitySequenceOrdinal)
        {
            switch (responsibilitySequenceOrdinal)
            {
                case 1: return ResponsibilitySequenceType.Primary;
                case 2: return ResponsibilitySequenceType.Secondary;
                case 3: return ResponsibilitySequenceType.Tertiary;
                case 4: return ResponsibilitySequenceType.Four;
                case 5: return ResponsibilitySequenceType.Five;
                case 6: return ResponsibilitySequenceType.Six;
                case 7: return ResponsibilitySequenceType.Seven;
                case 8: return ResponsibilitySequenceType.Eight;
                case 9: return ResponsibilitySequenceType.Nine;
                default: return ResponsibilitySequenceType.Primary;
            }

        }
    }

    public static class ResponsibilitySequenceHelper
    {
        private static readonly Dictionary<ResponsibilitySequenceType, ResponsibilitySequenceType?> _previousSequenceMap =
            new Dictionary<ResponsibilitySequenceType, ResponsibilitySequenceType?>
            {
               { ResponsibilitySequenceType.Tertiary, ResponsibilitySequenceType.Secondary },
               { ResponsibilitySequenceType.Secondary, ResponsibilitySequenceType.Primary },
               { ResponsibilitySequenceType.Primary, null },
               { ResponsibilitySequenceType.Four, ResponsibilitySequenceType.Tertiary },
               { ResponsibilitySequenceType.Five, ResponsibilitySequenceType.Four },
               { ResponsibilitySequenceType.Six, ResponsibilitySequenceType.Five },
               { ResponsibilitySequenceType.Seven, ResponsibilitySequenceType.Six },
               { ResponsibilitySequenceType.Eight, ResponsibilitySequenceType.Seven },
               { ResponsibilitySequenceType.Nine, ResponsibilitySequenceType.Eight }
            };

        public static ResponsibilitySequenceType? GetPreviousSequence(ResponsibilitySequenceType currentSequence)
        {
            return _previousSequenceMap.TryGetValue(currentSequence, out var previousSequence) ? previousSequence : null;
        }
        public static ResponsibilitySequenceType? GetCurrentSequence(ResponsibilitySequenceType currentSequence)
        {
            return _previousSequenceMap.TryGetValue(currentSequence, out var previousSequence) ? currentSequence : null;
        }

        private static readonly Dictionary<Type, Dictionary<string, Enum>> EnumMappings = new();

        public static T? GetEnumFromString<T>(string value) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
                return null;
            var type = typeof(T);
            // Check if enum mappings are already cached
            if (!EnumMappings.ContainsKey(type))
            {
                EnumMappings[type] = Enum.GetValues(type)
                    .Cast<T>()
                    .ToDictionary(e => e.GetEnumMemberValue(), e => (Enum)e);
            }
            return EnumMappings[type].TryGetValue(value, out var enumValue) ? (T)enumValue : null;
        }
        public static string GetEnumMemberValue<T>(this T enumValue) where T : Enum
        {
            var type = typeof(T);
            var memberInfo = type.GetMember(enumValue.ToString()).FirstOrDefault();
            var attribute = memberInfo?.GetCustomAttribute<EnumMemberAttribute>();
            return attribute?.Value ?? enumValue.ToString();
        }

    }
}
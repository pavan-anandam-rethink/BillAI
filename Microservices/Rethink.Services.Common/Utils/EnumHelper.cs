using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rethink.Services.Common.Utils
{
    [ExcludeFromCodeCoverage]
    public static class EnumHelper
    {
        /// <summary>
        /// Retrieve the description on the enum, e.g.
        /// [Description("Bright Pink")]
        /// BrightPink = 2,
        /// Then when you pass in the enum, it will retrieve the description
        /// </summary>
        /// <param name="en">The Enumeration</param>
        /// <returns>A string representing the friendly name</returns>
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                    typeof(DescriptionAttribute),
                    false);

            if (attributes.Length > 0)
                return attributes[0].Description;
            return value.ToString();
        }

        public static Expression<Func<TSource, int>> DescriptionOrder<TSource, TEnum>(
            Expression<Func<TSource, TEnum>> source)
            where TEnum : struct
        {
            var enumType = typeof(TEnum);
            if (!enumType.IsEnum) throw new InvalidOperationException();

            var body = ((TEnum[])Enum.GetValues(enumType))
                .OrderBy(value => value.GetDescription())
                .Select((value, ordinal) => new { value, ordinal })
                .Reverse()
                .Aggregate((Expression)null, (next, item) => next == null
                    ?
                    Expression.Constant(item.ordinal)
                    : Expression.Condition(
                        Expression.Equal(source.Body, Expression.Constant(item.value)),
                        Expression.Constant(item.ordinal),
                        next));

            return Expression.Lambda<Func<TSource, int>>(body, source.Parameters[0]);
        }

        public static string GetDescription<TEnum>(this TEnum value)
            where TEnum : struct
        {
            var enumType = typeof(TEnum);
            if (!enumType.IsEnum) throw new InvalidOperationException();

            var name = Enum.GetName(enumType, value);
            var field = typeof(TEnum).GetField(name, BindingFlags.Static | BindingFlags.Public);
            return field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
        }
    }
}
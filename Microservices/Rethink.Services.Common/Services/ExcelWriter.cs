using System;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Services
{
    [ExcludeFromCodeCoverage]
    public static class ExcelWriter
    {
        [AttributeUsage(AttributeTargets.Property, Inherited = true)]
        public class ExcludeAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = true)]
        public class BillingAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = true)]
        public class KareoAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = true)]
        public class AzaleaAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = true)]
        public class NotKareo : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = true)]
        public class ColumnSettingsAttribute : Attribute
        {
            public readonly string ColumnName;
            public readonly bool WrapText;
            public readonly bool AddNumberFormat;
            public readonly bool AddDateFormat;

            public ColumnSettingsAttribute(string columnName, bool wrapText = false, bool addNumberFormat = false, bool addDateFormat = false)
            {
                this.ColumnName = columnName;
                this.WrapText = wrapText;
                this.AddNumberFormat = addNumberFormat;
                this.AddDateFormat = addDateFormat;
            }
        }

    }
}
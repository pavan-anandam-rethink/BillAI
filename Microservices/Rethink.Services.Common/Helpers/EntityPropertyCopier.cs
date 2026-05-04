using Rethink.Services.Common.Entities.Base;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rethink.Services.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class EntityPropertyCopier
    {
        private static List<string> _propertyNamesToSkip = new List<string>()
            {
                "id",
                "createdby",
                "modifiedby",
                "datecreated",
                "datelastmodified",
                "datedeleted",
            };
        public static void Copy<T>(T class1, T class2, List<string> propertiesToSkip = null) where T : BasePersistEntity
        {
            var class1Properties = class1.GetType().GetProperties();
            var class2Properties = class2.GetType().GetProperties();

            foreach (var class1Property in class1Properties)
            {
                if (_propertyNamesToSkip.Contains(class1Property.Name.ToLower()) ||
                    (propertiesToSkip != null && propertiesToSkip.Any(ps => ps.ToLower().Equals(class1Property.Name.ToLower()))))
                {
                    continue;
                }
                foreach (var class2Property in class2Properties)
                {
                    if (class1Property.Name == class2Property.Name && class1Property.PropertyType == class2Property.PropertyType)
                    {
                        class2Property.SetValue(class2, class1Property.GetValue(class1));
                        break;
                    }
                }
            }
        }
    }
}

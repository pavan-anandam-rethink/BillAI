using Dapper;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rethink.Services.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class ColumnAttributeTypeMapper<T> : FallBackTypeMapper
    {
        public ColumnAttributeTypeMapper()
            : base(new SqlMapper.ITypeMap[]
            {
                new CustomPropertyTypeMap(typeof(T),
                    (type, columnName) =>
                        type.GetProperties().FirstOrDefault(prop =>
                            prop.GetCustomAttributes(false)
                                .OfType<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
                                .Any(attribute => attribute.Name == columnName)
                        )
                ),
                new DefaultTypeMap(typeof(T))
            })
        {
        }
    }
}
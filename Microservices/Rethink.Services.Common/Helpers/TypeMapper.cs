using Dapper;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rethink.Services.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public static class TypeMapper
    {
        public static void Initialize(string @namespace)
        {
            var types = from assem in AppDomain.CurrentDomain.GetAssemblies().ToList()
                        from type in assem.GetTypes()
                        where type.IsClass && type.Namespace == @namespace
                        select type;

            types.ToList().ForEach(type =>
            {
                var mapper = (SqlMapper.ITypeMap)Activator
                    .CreateInstance(typeof(ColumnAttributeTypeMapper<>)
                        .MakeGenericType(type));
                SqlMapper.SetTypeMap(type, mapper);
            });
        }
    }
}
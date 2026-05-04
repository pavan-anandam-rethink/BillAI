using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Utils
{
    [ExcludeFromCodeCoverage]
    public static class DbTranslateExt
    {
        public static List<T> Translate<T>(this DbDataReader reader) where T : class
        {
            var entityList = new List<T>();
            if (reader == null || reader.HasRows == false) return entityList;

            var firstLine = true;
            var resultRows = new HashSet<string>();
            while (reader.Read())
            {
                if (firstLine)
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        resultRows.Add(reader.GetName(i).ToLower());
                    }

                    firstLine = false;
                }

                if (typeof(T) == typeof(Dictionary<string, object>))
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in resultRows)
                    {
                        dict.Add(prop, reader[prop]);
                    }

                    entityList.Add(dict as T);
                }
                else
                {
                    var obj = Activator.CreateInstance<T>();
                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        if (!resultRows.Contains(prop.Name.ToLower())) continue;

                        if (!object.Equals(reader[prop.Name], DBNull.Value))
                        {
                            prop.SetValue(obj, reader[prop.Name], null);
                        }
                    }


                    entityList.Add(obj);
                }
            }

            return entityList;
        }
    }
}
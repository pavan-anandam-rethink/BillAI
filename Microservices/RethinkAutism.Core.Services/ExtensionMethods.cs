using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Rethink.Services.Common.Models;

namespace RethinkAutism.Core.Services
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }

            return dt.AddDays(-1 * diff).Date;
        }
    }

    public static partial class ExtensionMethods
    {
        public static void Decrypt<T>(this List<T> target) where T : EncryptionBase
        {
            Encryption.Decrypt(target);
        }
        public static void Encrypt<T>(this List<T> target) where T : EncryptionBase
        {
            Encryption.Encrypt(target);
        }

        public static Stream ToCSV(this DataTable table, bool includeHeader = true, string delimiter = ",")
        {

            StringBuilder result = new StringBuilder();
            if (includeHeader)
            {

                foreach (DataColumn column in table.Columns)
                {
                    result.Append(column.ColumnName);
                    result.Append(delimiter);
                }
                result.Remove(--result.Length, 0);
                result.Append(Environment.NewLine);
            }

            foreach (DataRow row in table.Rows)
            {
                foreach (object item in row.ItemArray)
                {
                    if (item is System.DBNull)
                        result.Append(delimiter);
                    else
                    {
                        string itemAsString = item.ToString();
                        itemAsString = itemAsString.Replace("\"", "\"\"");
                        itemAsString = "\"" + itemAsString + "\"";
                        result.Append(itemAsString + delimiter);
                    }
                }
                result.Remove(--result.Length, 0);
                result.Append(Environment.NewLine);

            }

            var ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(result.ToString());
            writer.Flush();
            ms.Position = 0;
            return ms;
        }
        public static T ValueorDefault<T>(this T? target) where T : struct, IComparable
        {
            if (target.HasValue)
            {
                return target.Value;
            }
            return default(T);
        }
        public static string ValueToString<T>(this T? target) where T : struct,IComparable
        {
            if (target.HasValue)
            {
                return target.Value.ToString();
            }
            return "";
        }

        public static void AddDistinct<T>(this ICollection<T> target, T item)
        {
            if (!target.Contains(item))
            {
                target.Add(item);
            }
        }
        public static void AddRangeDistinct<T>(this ICollection<T> target, IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                target.AddDistinct(i);
            }
        }
        public static void AddDistinct<T1, T2>(this IDictionary<T1, T2> target, T1 key, T2 value)
        {
            if (!target.ContainsKey(key))
            {
                target.Add(key, value);
            }
        }
        public static string MaxLength(this string target, int MaxLength)
        {
            if (target.Length > MaxLength)
            {
                return target.Substring(0, MaxLength);
            }
            return target;
        }

        public static List<dynamic> toExpandoList(this IEnumerable<object> target, string Mapping = "")
        {
            var List = new List<dynamic>();
            Dictionary<string, string> Map = null;
            foreach (var li in target)
            {
                var e = new System.Dynamic.ExpandoObject();
                Map = e.SetValuesFrom(li, Mapping, true, Map);
                List.Add(e);
            }
            return List;
        }
        public static Dictionary<string, string> SetValuesFrom(this object target, object source, string Mapping = "", bool SetNulls = false, Dictionary<string, string> Map = null)
        {
            if (Map == null)
            {
                Map = new Dictionary<string, string>();
                if (Mapping != "")
                {
                    var arrMapping = Mapping.Split(',');
                    foreach (var s in arrMapping)
                    {
                        if (s.IndexOf(":") > -1)
                        {
                            var sSplit = s.Split(':');
                            Map.Add(sSplit[0], sSplit[1]);
                        }
                        else
                        {
                            Map.Add(s, s);
                        }
                    }
                }
                else
                {
                    source.GetType().GetProperties().Select(pinfo => pinfo.Name).ToList().ForEach(n => Map.Add(n, n));

                }
            }
            var Setter = FastMember.ObjectAccessor.Create(target);
            var Getter = FastMember.ObjectAccessor.Create(source);
            var targetProperties = target.GetType().GetProperties().Where(p => p.CanWrite).Select(p => p.Name).ToList();
            foreach (var m in Map)
            {
                if (targetProperties.Contains(m.Value) || target.ToString() == "System.Dynamic.ExpandoObject")
                {
                    var val = Getter[m.Key];
                    if (SetNulls == true || (val != null && !val.Equals(GetDefault(val.GetType()))))
                    {
                        Setter[m.Value] = val;
                    }
                }
            }
            return Map;
        }
        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static Int32 GetAge(this DateTime dateOfBirth)
        {
            var today = DateTime.Today;

            var a = (today.Year * 100 + today.Month) * 100 + today.Day;
            var b = (dateOfBirth.Year * 100 + dateOfBirth.Month) * 100 + dateOfBirth.Day;

            return (a - b) / 10000;
        }

        public static List<T> ConvertToEntityList<T>(this DataTable table) where T : class, new()
        {
            var retList = new List<T>();
            foreach (System.Data.DataRow dr in table.Rows)
            {
                retList.Add(dr.ConvertToEntity<T>());
            }
            return retList;
        }
        public static T ConvertToEntity<T>(this DataRow tableRow) where T : class, new()
        {
            Type t = typeof(T);
            T returnObject = new T();

            foreach (DataColumn col in tableRow.Table.Columns)
            {
                string colName = col.ColumnName;

                // Look for the object's property with the columns name, ignore case
                PropertyInfo pInfo = t.GetProperty(colName.ToLower(),
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                // did we find the property ?
                if (pInfo != null)
                {
                    object val = tableRow[colName];

                    // is this a Nullable<> type
                    bool IsNullable = (Nullable.GetUnderlyingType(pInfo.PropertyType) != null);
                    if (IsNullable)
                    {
                        if (val is System.DBNull)
                        {
                            val = null;
                        }
                        else
                        {
                            // Convert the db type into the T we have in our Nullable<T> type
                            val = Convert.ChangeType
                    (val, Nullable.GetUnderlyingType(pInfo.PropertyType));
                        }
                    }
                    else
                    {
                        // Convert the db type into the type of the property in our entity
                        var dbnull = val as DBNull;
                        if (dbnull == null)
                        {
                            val = Convert.ChangeType(val, pInfo.PropertyType);
                        }
                        else
                        {
                            val = null;
                        }

                    }
                    // Set the value of the property with the value from the db
                    pInfo.SetValue(returnObject, val, null);
                }
            }

            // return the entity object with values
            return returnObject;
        }

        public static MemoryStream ToCsv(this DataTable table, bool includeHeader = true, string delimiter = ",", string dateFormat = null, string timeFormat = null)
        {
            timeFormat = timeFormat.Replace("A", "tt");
            var result = new StringBuilder();
            if (includeHeader)
            {

                foreach (DataColumn column in table.Columns)
                {
                    result.Append(column.ColumnName);
                    result.Append(delimiter);
                }
                result.Remove(--result.Length, 0);
                result.Append(Environment.NewLine);
            }

            foreach (DataRow row in table.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    if (item is DBNull)
                        result.Append(delimiter);
                    else
                    {
                        var itemAsString = item.ToString();
                        if (DateTime.TryParseExact(itemAsString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                        {
                            var regDate = "\\d{1,2}/\\d{1,2}/\\d{4}";
                            if (System.Text.RegularExpressions.Regex.IsMatch(itemAsString, regDate))
                            {
                                itemAsString = date.ToString(dateFormat);
                            }
                            else
                            {
                                itemAsString = date.ToString(timeFormat);
                            }
                        }
                        itemAsString = itemAsString.Replace("\"", "\"\"");
                        itemAsString = "\"" + itemAsString + "\"";
                        result.Append(itemAsString + delimiter);
                    }
                }
                result.Remove(--result.Length, 0);
                result.Append(Environment.NewLine);

            }

            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(result.ToString());
            writer.Flush();
            ms.Position = 0;
            return ms;
        }

        public static T Deserialize<T>(this string target)
        {
            var JSONsettings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            };
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(target, JSONsettings);
        }

        public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
        {
            List<T> list = destination as List<T>;

            if (list != null)
            {
                list.AddRange(source);
            }
            else
            {
                foreach (T item in source)
                {
                    destination.Add(item);
                }
            }
        }

        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source,
           List<SortingModel> sortingModels)
        {
            for (var i = 0; i < sortingModels.Count; i++)
            {
                var sortingModel = sortingModels[i];
                if (string.IsNullOrWhiteSpace(sortingModel.Dir))
                    break;

                var isDesc = sortingModel.Dir.Equals("desc", StringComparison.InvariantCultureIgnoreCase);
                source = source.OrderBy(sortingModel.Field, isDesc, i != 0);
            }

            return source;
        }

        private static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source,
            string orderByProperty, bool desc, bool isSecondary = false)
        {
            if (string.IsNullOrWhiteSpace(orderByProperty))
            {
                return source;
            }

            var command = desc ?
                isSecondary ? "ThenByDescending" : "OrderByDescending" :
                isSecondary ? "ThenBy" : "OrderBy";


            var type = typeof(TEntity);
            var property = type.GetProperty(orderByProperty, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);


            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
                source.Expression, Expression.Quote(orderByExpression));

            return (IOrderedQueryable<TEntity>)source.Provider.CreateQuery<TEntity>(resultExpression);
        }
    }
}
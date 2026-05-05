using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rethink.Services.Common.Utils
{
    [ExcludeFromCodeCoverage]
    public static class LinqExtensions
    {
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


        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source,
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


        public static IQueryable<TEntity> Filter<TEntity>(this IQueryable<TEntity> source, List<FilterModel> filters)
        {
            if (filters == null || filters.Count == 0)
            {
                return source;
            }

            var type = typeof(TEntity);
            var xParameter = Expression.Parameter(type, "x");
            Expression finalExpression = null;

            foreach (var filter in filters)
            {
                var property = type.GetProperty(filter.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                var xProperty = Expression.Property(xParameter, filter.PropertyName);

                Expression expression = null;

                MethodInfo methodInfo;
                ConstantExpression constraintException;

                List<Expression> expressions;

                object parsedFilterValue = null;
                List<object> parsedFilterValues = null;
                if (filter.OperatorName.Contains("any") || filter.OperatorName.Contains("range"))
                {
                    parsedFilterValues = filter.Value.Split(',').Select(v => ChangeType(v, property.PropertyType)).ToList();
                }
                else
                {
                    parsedFilterValue = ChangeType(filter.Value, property.PropertyType);
                }

                switch (filter.OperatorName)
                {
                    case "eq":
                        expression = Expression.Equal(xProperty, Expression.Constant(parsedFilterValue));
                        break;
                    case "lt":
                        expression = Expression.LessThan(xProperty, Expression.Constant(parsedFilterValue));
                        break;
                    case "gt":
                        expression = GreaterThan(xProperty, Expression.Constant(parsedFilterValue));
                        break;
                    case "lte":
                        expression = Expression.LessThanOrEqual(xProperty, Expression.Constant(parsedFilterValue));
                        break;
                    case "gte":
                        expression = Expression.GreaterThanOrEqual(xProperty, Expression.Constant(parsedFilterValue));
                        break;
                    case "eqdate":
                        var dateColleance = Expression.Coalesce(xProperty, Expression.Constant(SqlDateTime.MinValue.Value));
                        var xDateProperty = Expression.Property(dateColleance, "Date");
                        expression = Expression.Equal(xDateProperty, Expression.Constant(((DateTime)parsedFilterValue).Date));
                        break;
                    case "rangedate":
                        var colleance = Expression.Coalesce(xProperty, Expression.Constant(SqlDateTime.MinValue.Value));
                        var xDateRangeProperty = Expression.Property(colleance, "Date");
                        var leftExpression = Expression.GreaterThanOrEqual(xDateRangeProperty, Expression.Constant(((DateTime)parsedFilterValues[0]).Date));
                        var rightExpression = Expression.LessThanOrEqual(xDateRangeProperty, Expression.Constant(((DateTime)parsedFilterValues[1]).Date));
                        expression = Expression.And(leftExpression, rightExpression);
                        break;
                    case "contains":
                        methodInfo = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                        expression = Expression.Call(xProperty, methodInfo);

                        constraintException = Expression.Constant(filter.Value, typeof(string));
                        methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                        expression = Expression.Call(expression, methodInfo, constraintException);
                        break;
                    case "startswith":
                        methodInfo = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
                        constraintException = Expression.Constant(filter.Value, typeof(string));
                        expression = Expression.Call(xProperty, methodInfo, constraintException);
                        break;
                    case "eqany":
                        expressions = parsedFilterValues.Select(v => (Expression)Expression.Equal(xProperty, Expression.Constant(v))).ToList();
                        expression = expressions[0];
                        for (int i = 1; i < expressions.Count; i++)
                        {
                            expression = Expression.OrElse(expression, expressions[i]);
                        }
                        break;
                    case "startswithany":
                        methodInfo = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });

                        expressions = parsedFilterValues.Select(v =>
                        {
                            constraintException = Expression.Constant(v, typeof(string));
                            return (Expression)Expression.Call(xProperty, methodInfo, constraintException);
                        }).ToList();
                        expression = expressions[0];
                        for (int i = 1; i < expressions.Count; i++)
                        {
                            expression = Expression.OrElse(expression, expressions[i]);
                        }
                        break;
                    case "containsany":
                        methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                        expressions = parsedFilterValues.Select(v =>
                        {
                            constraintException = Expression.Constant(v, typeof(string));
                            return (Expression)Expression.Call(xProperty, methodInfo, constraintException);
                        }).ToList();
                        expression = expressions[0];
                        for (int i = 1; i < expressions.Count; i++)
                        {

                            expression = Expression.OrElse(expression, expressions[i]);
                        }
                        break;

                    default:
                        continue;
                }


                finalExpression = finalExpression == null
                    ? expression
                    : Expression.AndAlso(finalExpression, expression);
            }


            var lambdaExpression = Expression.Lambda<Func<TEntity, bool>>(finalExpression, xParameter);

            return source.Where(lambdaExpression);
        }

        private static object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }
            if (conversion.IsEnum)
                return Enum.Parse(conversion, (string)value);

            return Convert.ChangeType(value, t);
        }

        static Expression GreaterThan(Expression e1, Expression e2)
        {
            if (IsNullableType(e1.Type) && !IsNullableType(e2.Type))
                e2 = Expression.Convert(e2, e1.Type);
            else if (!IsNullableType(e1.Type) && IsNullableType(e2.Type))
                e1 = Expression.Convert(e1, e2.Type);
            return Expression.GreaterThan(e1, e2);
        }
        static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        //help method
        public static string ToSql<TEntity>(this IQueryable<TEntity> query) where TEntity : class
        {
            using var enumerator = query.Provider.Execute<IEnumerable<TEntity>>(query.Expression).GetEnumerator();
            var relationalCommandCache = enumerator.Private("_relationalCommandCache");
            var selectExpression = relationalCommandCache.Private<SelectExpression>("_selectExpression");
            var factory = relationalCommandCache.Private<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");

            var sqlGenerator = factory.Create();
            var command = sqlGenerator.GetCommand(selectExpression);

            string sql = command.CommandText;
            return sql;
        }

        private static object Private(this object obj, string privateField) => obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        private static T Private<T>(this object obj, string privateField) => (T)obj?.GetType().GetField(privateField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
    }
}
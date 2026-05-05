using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Extensions
{
    public static class SortingExtensions
    {
        public static IEnumerable<T> ApplySorting<T>(
            this IEnumerable<T> source,
            List<SortingModel> sortingModels)
        {
            if (sortingModels == null || !sortingModels.Any())
                return source;

            IOrderedEnumerable<T> orderedQuery = null;

            foreach (var sort in sortingModels)
            {
                var propertyInfo = typeof(T)
                    .GetProperties()
                    .FirstOrDefault(p =>
                        p.Name.Equals(sort.Field, StringComparison.OrdinalIgnoreCase));

                if (propertyInfo == null)
                    continue;

                if (orderedQuery == null)
                {
                    orderedQuery = sort.Dir.ToLower() == "desc"
                        ? source.OrderByDescending(x => propertyInfo.GetValue(x, null))
                        : source.OrderBy(x => propertyInfo.GetValue(x, null));
                }
                else
                {
                    orderedQuery = sort.Dir.ToLower() == "desc"
                        ? orderedQuery.ThenByDescending(x => propertyInfo.GetValue(x, null))
                        : orderedQuery.ThenBy(x => propertyInfo.GetValue(x, null));
                }
            }

            return orderedQuery ?? source;
        }
    }
}

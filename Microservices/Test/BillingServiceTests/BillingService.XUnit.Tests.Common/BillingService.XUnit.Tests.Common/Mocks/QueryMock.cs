using System.Collections.Generic;
using System.Linq;

namespace BillingService.XUnit.Tests.Common.Mocks
{
    public static class QueryMock<T>
        where T : class
    {
        public static IQueryable<T> Create(IEnumerable<T> entities) => DbMock.Create(entities.ToList());

        public static IQueryable<T> Create(params T[] entities) => DbMock.Create(entities.ToList());
    }
}

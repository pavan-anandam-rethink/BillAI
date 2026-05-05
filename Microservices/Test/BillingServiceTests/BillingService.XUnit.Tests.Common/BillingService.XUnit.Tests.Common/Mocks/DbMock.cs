using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BillingService.XUnit.Tests.Common.Mocks
{
    public static class DbMock
    {
        public static DbSet<T> Create<T>(List<T> items)
            where T : class
        {
            var queryItems = items.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IAsyncEnumerable<T>>().Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryItems.GetEnumerator()));
            mockSet.As<IQueryable<T>>().Setup(x => x.Provider)
                .Returns(new TestAsyncQueryProvider(queryItems.Provider));
            mockSet.As<IQueryable<T>>().Setup(x => x.Expression)
                .Returns(queryItems.Expression);
            mockSet.As<IQueryable<T>>().Setup(x => x.ElementType)
                .Returns(queryItems.ElementType);
            mockSet.As<IQueryable<T>>().Setup(x => x.GetEnumerator())
                .Returns(queryItems.GetEnumerator());
            mockSet.Setup(x => x.Remove(It.IsAny<T>()))
                .Callback<T>(entity => { items.Remove(entity); });

            return mockSet.Object;
        }
    }
}

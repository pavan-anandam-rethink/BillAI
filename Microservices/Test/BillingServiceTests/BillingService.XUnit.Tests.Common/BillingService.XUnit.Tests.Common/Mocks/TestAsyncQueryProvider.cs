using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.XUnit.Tests.Common.Mocks
{
    public class TestAsyncQueryProvider : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression.Type.GenericTypeArguments.Length == 1)
            {
                var asyncEnumerableType = typeof(TestAsyncEnumerable<>).MakeGenericType(expression.Type.GenericTypeArguments);
                var asyncEnumerable = Activator.CreateInstance(asyncEnumerableType, expression);
                return (IQueryable)asyncEnumerable;
            }

            throw new Exception("Unknown query with cast");
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(
                                      name: nameof(IQueryProvider.Execute),
                                      genericParameterCount: 1,
                                      types: new[] { typeof(Expression) })
                                 .MakeGenericMethod(expectedResultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                                        ?.MakeGenericMethod(expectedResultType)
                                         .Invoke(null, new[] { executionResult });
        }
    }
}

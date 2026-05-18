using System.Linq.Expressions;

namespace BillingService.Application.Abstractions.Persistence;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    IReadOnlyCollection<Expression<Func<T, object>>> Includes { get; }

    bool AsNoTracking { get; }
}

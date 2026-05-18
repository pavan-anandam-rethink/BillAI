using System.Linq.Expressions;

namespace BillingService.Application.Abstractions.Persistence;

public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];

    public Expression<Func<T, bool>>? Criteria { get; private set; }

    public IReadOnlyCollection<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    public bool AsNoTracking { get; private set; }

    protected void Where(Expression<Func<T, bool>> criteria) => Criteria = criteria;

    protected void Include(Expression<Func<T, object>> includeExpression) => _includes.Add(includeExpression);

    protected void UseNoTracking() => AsNoTracking = true;
}

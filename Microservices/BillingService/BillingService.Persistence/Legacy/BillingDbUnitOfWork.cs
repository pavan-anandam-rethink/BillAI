using BillingService.Application.Abstractions.Persistence;
using Rethink.Services.Common.Infrastructure.Context.Billing;

namespace BillingService.Persistence.Legacy;

public sealed class BillingDbUnitOfWork(BillingDbContext billingDbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return billingDbContext.SaveChangesAsync(cancellationToken);
    }
}

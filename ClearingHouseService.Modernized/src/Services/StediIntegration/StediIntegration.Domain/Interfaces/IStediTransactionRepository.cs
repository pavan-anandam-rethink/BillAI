using ClearingHouse.SharedKernel.Interfaces;
using StediIntegration.Domain.Entities;

namespace StediIntegration.Domain.Interfaces;

public interface IStediTransactionRepository : IRepository<StediTransaction>
{
    Task<StediTransaction?> GetByStediTransactionIdAsync(string stediTransactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StediTransaction>> GetPendingTransactionsAsync(CancellationToken cancellationToken = default);
}

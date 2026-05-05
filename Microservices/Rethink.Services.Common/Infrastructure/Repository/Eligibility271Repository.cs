using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Repository
{
    public class Eligibility271Repository : IEligibility271Repository
    {
        private readonly BillingDbContext _dbContext;
        private readonly ILogger<Eligibility271Repository> _logger;
        public Eligibility271Repository(
            BillingDbContext dbContext,
            ILogger<Eligibility271Repository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SaveAsync(Eligibility271ResponseEntity eligibility271ResponseEntity,CancellationToken cancellationToken)
        {
            _logger.LogInformation("SaveAsync Eligibility 271. TransactionControlNumber={TransactionControlNumber}", eligibility271ResponseEntity.TransactionControlNumber);

            _dbContext.Eligibility271Responses.Add(eligibility271ResponseEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Eligibility 271 Saved successfully. TransactionControlNumber={TransactionControlNumber}", eligibility271ResponseEntity.TransactionControlNumber);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Repository
{
    public class ClaimRepository : BaseService, IClaimRepository
    {
        private readonly BillingDbContext _dbContext;
        private readonly ILogger<ClaimRepository> _logger;

        public ClaimRepository(BillingDbContext dbContext, ILogger<ClaimRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task UpdateClaimDetailsAsync(int claimId, ClaimStatus status, string error = null)
        {
            var claim = await _dbContext.Claims.FindAsync(claimId);

            if (claim == null)
            {
                _logger.LogWarning("Claim not found: {ClaimId}", claimId);
                return;
            }

            claim.ClaimStatus = status;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Claim status updated. ClaimId: {ClaimId}, Status: {Status}", claimId, status);
        }

        public async Task<string> GetErrorMessageAsync(ClaimErrorNumber errorNumber)
        {
            var errorMessage = await _dbContext.ClaimErrorMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ErrorNumber == errorNumber && e.DateDeleted == null);

            if (errorMessage == null)
            {
                _logger.LogWarning("Error message not found for ErrorNumber: {ErrorNumber}", errorNumber);
                return null;
            }

            return errorMessage.LongDescription ?? errorMessage.ShortDescription;
        }

        public async Task<int?> GetErrorMessageIdAsync(ClaimErrorNumber errorNumber)
        {
            var errorMessage = await _dbContext.ClaimErrorMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ErrorNumber == errorNumber && e.DateDeleted == null);

            if (errorMessage == null)
            {
                _logger.LogWarning("Error message not found for ErrorNumber: {ErrorNumber}", errorNumber);
                return null;
            }

            return errorMessage.Id;
        }

        public async Task<int?> GetLatestClaimSubmissionIdAsync(int claimId)
        {
            // Try to get the claim submission for this specific claim
            var claimSubmission = await _dbContext.ClaimSubmissions
                .AsNoTracking()
                .Where(cs => cs.ClaimId == claimId && cs.DateDeleted == null)
                .OrderByDescending(cs => cs.Id)
                .Select(cs => cs.Id)
                .FirstOrDefaultAsync();

            if (claimSubmission != 0)
            {
                return claimSubmission;
            }

            // If no submission for this claim, get the latest one (for logging purposes)
            var latestSubmission = await _dbContext.ClaimSubmissions
                .AsNoTracking()
                .Where(cs => cs.DateDeleted == null)
                .OrderByDescending(cs => cs.Id)
                .Select(cs => cs.Id)
                .FirstOrDefaultAsync();

            return latestSubmission != 0 ? latestSubmission : null;
        }

        public async Task SaveClaimValidationErrorAsync(int claimId, int claimSubmissionId, int errorMessageId, string contextMessage, ClaimErrorSource errorSource)
        {
            try
            {
                // Check if the error already exists for the given claim and error message
                var existingError = await _dbContext.ClaimValidationErrors
                    .FirstOrDefaultAsync(ce =>
                        ce.ClaimId == claimId &&
                        ce.ClaimErrorMessageId == errorMessageId &&
                        ce.ClaimErrorSource == errorSource &&
                        ce.DateDeleted == null);

                if (existingError != null)
                {
                    _logger.LogInformation("Claim validation error already exists. ClaimId: {ClaimId}, ErrorMessageId: {ErrorMessageId}", claimId, errorMessageId);
                    return;
                }

                var claimError = new ClaimValidationErrorEntity
                {
                    ClaimSubmissionId = claimSubmissionId,
                    ClaimId = claimId,
                    ClaimErrorMessageId = errorMessageId,
                    ClaimErrorSource = errorSource,
                    ContextMessage = contextMessage,
                    ValidationDate = EstDateTime,
                    DateCreated = EstDateTime,
                    CreatedBy = 0 // System created
                };

                await _dbContext.ClaimValidationErrors.AddAsync(claimError);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Claim validation error saved. ClaimId: {ClaimId}, ErrorMessageId: {ErrorMessageId}", claimId, errorMessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving claim validation error. ClaimId: {ClaimId}, ErrorMessageId: {ErrorMessageId}", claimId, errorMessageId);
            }
        }
    }
}

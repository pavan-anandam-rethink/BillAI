using ClearingHouseService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Domain.Services
{
    /// <summary>
    /// Default EDI validator implementation.
    /// Performs basic structural validation of EDI content.
    /// Can be extended or replaced with more comprehensive validation logic.
    /// </summary>
    public class DefaultEdiValidator : IEdiValidator
    {
        private readonly ILogger<DefaultEdiValidator> _logger;

        public DefaultEdiValidator(ILogger<DefaultEdiValidator> logger)
        {
            _logger = logger;
        }

        public Task<EdiValidationResult> ValidateAsync(string ediContent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ediContent))
            {
                _logger.LogWarning("EDI content is empty or null");
                return Task.FromResult(EdiValidationResult.Invalid("EDI content cannot be empty"));
            }

            // Basic structural validation - check for required EDI segments
            if (!ediContent.Contains("ISA") || !ediContent.Contains("IEA"))
            {
                _logger.LogWarning("EDI content missing required ISA/IEA envelope segments");
                return Task.FromResult(EdiValidationResult.Invalid(
                    "EDI content must contain ISA (Interchange Control Header) and IEA (Interchange Control Trailer) segments"));
            }

            if (!ediContent.Contains("GS") || !ediContent.Contains("GE"))
            {
                _logger.LogWarning("EDI content missing required GS/GE functional group segments");
                return Task.FromResult(EdiValidationResult.Invalid(
                    "EDI content must contain GS (Functional Group Header) and GE (Functional Group Trailer) segments"));
            }

            _logger.LogDebug("EDI content passed basic structural validation");
            return Task.FromResult(EdiValidationResult.Valid());
        }
    }
}

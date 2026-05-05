using BillingService.Domain.Interfaces.Billing;
using Microsoft.Extensions.Logging;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class FeatureFlagService : IFeatureFlagService
    {
        private const string EnableProviderEnrollmentValidationKey = "EnableProviderEnrollmentValidation";

        private readonly IKeyVaultProviderService _keyVaultProviderService;
        private readonly ILogger<FeatureFlagService> _logger;

        public FeatureFlagService(
            IKeyVaultProviderService keyVaultProviderService,
            ILogger<FeatureFlagService> logger)
        {
            _keyVaultProviderService = keyVaultProviderService;
            _logger = logger;
        }

        public async Task<bool> IsProviderEnrollmentValidationEnabledAsync()
        {
            try
            {
                var secretValue = await _keyVaultProviderService.GetSecretAsync(EnableProviderEnrollmentValidationKey);

                if (bool.TryParse(secretValue, out var parsedValue))
                {
                    _logger.LogDebug(
                        "Feature flag '{FlagName}' loaded from Key Vault. Value={FlagValue}",
                        EnableProviderEnrollmentValidationKey, parsedValue);
                    return parsedValue;
                }

                _logger.LogWarning(
                    "Feature flag '{FlagName}' has unparseable value '{RawValue}'. Falling back to default={Default}",
                    EnableProviderEnrollmentValidationKey, secretValue, false);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to retrieve feature flag '{FlagName}' from Key Vault. Falling back to default={Default}",
                    EnableProviderEnrollmentValidationKey, false);
                return false;
            }
        }
    }
}

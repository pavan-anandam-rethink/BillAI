using Billing.FolderStructure.Core.Enum;
using ClearingHouseService.Web.Interface;
using Rethink.Services.Domain.Interfaces;

namespace ClearingHouseService.Web.Service
{
    // This class implements the ICredentialResolver interface to provide a mechanism for resolving credentials based on the clearing house details.
    // It retrieves the necessary credentials (such as URL, port, username, password, and remote directory)
    // from the configuration for specific clearing houses (e.g., Stedi, Availity) or uses the provided details for others.
    public class CredentialResolver : ICredentialResolver
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CredentialResolver> _logger;
        private readonly IKeyVaultProviderService _keyVaultProviderService;

        private static readonly Dictionary<BillingClearingHousesEnum, string> ClearingHouseConfigSections = new()
        {
            { BillingClearingHousesEnum.Stedi, "Clearinghouses:Stedi" },
            { BillingClearingHousesEnum.Availity, "Clearinghouses:Availity" }
        };

        public CredentialResolver(IConfiguration configuration, ILogger<CredentialResolver> logger, IKeyVaultProviderService keyVaultProviderService)
        {
            _configuration = configuration;
            _logger = logger;
            _keyVaultProviderService = keyVaultProviderService;
        }

        public async Task<ClearingHouseDetailsModel> ResolveAsync(ClearingHouseDetailsModel clearingHouse)
        {
            var clearingHouseType = (BillingClearingHousesEnum)clearingHouse.ClearingHouseId;

            _logger.LogInformation("ResolveAsync Called ClearingHouseId: {ClearingHouseId}", clearingHouse.ClearingHouseId);

            if (ClearingHouseConfigSections.TryGetValue(clearingHouseType, out var configSection))
            {
                return await ResolveFromKeyVaultAsync(configSection);
            }
            return clearingHouse;
        }

        private async Task<ClearingHouseDetailsModel> ResolveFromKeyVaultAsync(string configSection)
        {
            var hosts = await _keyVaultProviderService.GetSecretAsync(_configuration[$"{configSection}:Hosts"]);
            var portStr = await _keyVaultProviderService.GetSecretAsync(_configuration[$"{configSection}:Port"]);
            var userName = await _keyVaultProviderService.GetSecretAsync(_configuration[$"{configSection}:UserName"]);
            var userPassword = await _keyVaultProviderService.GetSecretAsync(_configuration[$"{configSection}:UserPassword"]);
            var uploadDirectory = await _keyVaultProviderService.GetSecretAsync(_configuration[$"{configSection}:UploadDirectory"]);
            var downloadDirectory = await _keyVaultProviderService.GetSecretAsync(_configuration[$"{configSection}:DownloadDirectory"]);

            return new ClearingHouseDetailsModel
            {
                UrlLink = hosts,
                Port = Convert.ToInt32(portStr),
                UserName = userName,
                UserPassword = userPassword,
                UploadDirectory = uploadDirectory,
                DownloadDirectory = downloadDirectory
            };
        }
    }
}

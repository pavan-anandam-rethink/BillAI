using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Interfaces;
using System;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services.RethinkServices
{
    public sealed class RethinkMasterDataSessionPrewarm : IRethinkMasterDataSessionPrewarm
    {
        private readonly IRethinkMasterDataMicroServices _masterData;
        private readonly IRethinkBillingRequestContext _requestContext;
        private readonly ILogger<RethinkMasterDataSessionPrewarm>? _logger;
        private readonly bool _enabled;

        public RethinkMasterDataSessionPrewarm(
            IRethinkMasterDataMicroServices masterData,
            IRethinkBillingRequestContext requestContext,
            IConfiguration configuration,
            ILogger<RethinkMasterDataSessionPrewarm>? logger = null)
        {
            _masterData = masterData;
            _requestContext = requestContext;
            _logger = logger;
            _enabled = configuration.GetSection("RethinkMasterDataSession").GetValue("PrewarmEnabled", true);
        }

        public async Task WarmAsync(int accountInfoId, string sessionKey)
        {
            if (!_enabled || accountInfoId <= 0 || string.IsNullOrWhiteSpace(sessionKey))
            {
                return;
            }

            _requestContext.SessionKey = sessionKey;
            _requestContext.AccountInfoId = accountInfoId;

            try
            {
                await Task.WhenAll(
                    _masterData.GetPlaceOfService(accountInfoId),
                    _masterData.GetLocationCodes(),
                    _masterData.GetClearingHouseDetails(),
                    _masterData.GetReasonCodes(),
                    _masterData.GetUnitTypesAsync(),
                    _masterData.GetTimezones(),
                    _masterData.GetStateList(),
                    _masterData.GetCountryList(),
                    _masterData.GetMemberListAsync(accountInfoId),
                    _masterData.GetChildProfile(accountInfoId),
                    _masterData.GetFunderList(accountInfoId),
                    _masterData.GetProviderLocationList(accountInfoId),
                    _masterData.GetRenderingProvidersAsync(accountInfoId, false),
                    _masterData.GetBillingCodeList(accountInfoId),
                    _masterData.GetMainLocation(accountInfoId),
                    _masterData.GetClientDetailsGuarantor(accountInfoId));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Master data session prewarm completed with partial failures for account {AccountId}", accountInfoId);
            }
        }
    }
}

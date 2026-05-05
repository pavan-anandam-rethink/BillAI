using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Clients
{
    public class ClaimCreateInfoModel
    {
        public List<AuthRenderingProviderType> RenderingProviders { get; set; }
        public List<ClientAuthorizationBillingCodeSmall> BillingCodes { get; set; }
        public List<ProviderLocationData> Locations { get; set; }
        public List<ClientReferringProviderForDropdownModel> ReferringProviders { get; set; }
    }
}

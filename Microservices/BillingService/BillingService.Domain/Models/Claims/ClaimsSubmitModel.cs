using Rethink.Services.Common.Enums.Billing;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimsSubmitModel : IdsWithUserInfo
    {
        public bool IsSecondary { get; set; } = false;
        public AdjustmentLevel? AdjustmentLevel { get; set; }
        public List<SecondaryFunderDetailsModel> SecondaryFunderDetails { get; set; }
        public string ImpersonationUserName { get; set; } = string.Empty;
    }
}

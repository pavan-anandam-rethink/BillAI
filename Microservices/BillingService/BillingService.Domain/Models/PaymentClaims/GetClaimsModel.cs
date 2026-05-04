using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class GetClaimsModel
    {
        public int PaymentId { get; set; }
        public List<SortingModel> SortingModels { get; set; }
        public List<FilterModel> FilterModels { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public bool ShowPaid { get; set; }
        public int AccountInfoId { get; set; } = 0;
    }

    public class GetClaimFilterModel : UserInfo
    {
        public int PaymentId { get; set; }
        public List<SortingModel> SortingModels { get; set; }
        public ClaimFilterModel FilterModels { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }

    }

    public class ClaimFilterModel
    {
        public string? ClientIds { get; set; } = string.Empty;
        public string? ClaimIdentifier { get; set; } = string.Empty;
        public decimal? PaidAmountFrom { get; set; } = null;
        public decimal? PaidAmountTo { get; set; } = null;
        public decimal? BalanceAmountFrom { get; set; } = null;
        public decimal? BalanceAmountTo { get; set; } = null;
        public bool? ShowPaid { get; set; }
    }
}
using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Billing
{
    [ExcludeFromCodeCoverage]
    public class ClaimChargeInfoItem
    {
        public string BillingCode { get; set; }

        public DateTime DateOfService { get; set; }

        public string Modifier1 { get; set; }

        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Modifier4 { get; set; }

        public decimal? Units { get; set; }

        public decimal? UnitRate { get; set; }

        public decimal? TotalCharge { get; set; }

        public decimal? TotalPaid { get; set; }
    }
}

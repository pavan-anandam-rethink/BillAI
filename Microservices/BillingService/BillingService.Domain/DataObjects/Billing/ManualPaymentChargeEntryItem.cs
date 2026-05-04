using Rethink.Services.Common.Entities.Billing.Payment;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.DataObjects.Billing
{
    public class ManualPaymentChargeEntryItem
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Charges { get; set; }
        public decimal Units { get; set; }
        public DateTime? DateOfService { get; set; }
        public string ServiceCode { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Modifier4 { get; set; }
        public string? Description { get; set; }
        public IEnumerable<ChargePaymentEntity> ClaimChargeItems { get; set; }
    }
}

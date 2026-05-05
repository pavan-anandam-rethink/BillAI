using System.Collections.Generic;

namespace BillingService.Domain.DataObjects.Billing
{
    public class ClaimChargeItem
    {
        public int ClaimId { get; set; }
        //for create era claim
        public int PatientId { get; set; }
        public int ClaimStatus { get; set; }
        //One claim -> many charges
        public IEnumerable<ManualPaymentChargeEntryItem> ChargeEntries { get; set; }
    }
}

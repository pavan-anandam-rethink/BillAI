using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ChargeTransactionEntity : BasePersistEntity
    {
        public int ClaimId { get; set; }
        public int ChargeEntryId { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal OtherPayment { get; set; }
        public decimal InsurancePayment { get; set; }
        public decimal PatientPayment { get; set; }
        public decimal WriteOff { get; set; }
        public decimal Adjustment { get; set; }
        public decimal PatientResponsibility { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}

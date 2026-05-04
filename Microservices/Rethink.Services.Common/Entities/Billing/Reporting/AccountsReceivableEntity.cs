using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Reporting
{
    public class AccountsReceivableEntity : BasePersistEntity
    {
        public int AccountInfoId { get; set; }
        public int ClaimId { get; set; }
        public int FunderId { get; set; }
        public int ClientId { get; set; }
        public int ClaimStatusId { get; set; }
        public DateTime ClaimFrom { get; set; }
        public DateTime ClaimThrough { get; set; }
        public DateTime? BilledDate { get; set; }
        public Decimal BilledAmount { get; set; }
        public Decimal PatientResponsibility { get; set; }
        public Decimal WriteOff { get; set; }
        public Decimal Adjustment { get; set; }
        public Decimal AdjustedClaimAmount { get; set; }
        public Decimal PaymentRecieved { get; set; }
        public Decimal NetRecievable { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}

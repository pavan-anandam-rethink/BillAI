using System;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class AccountsReceivableQueryModel
    {
        public string FunderName { get; set; }
        public int ClientId { get; set; }
        public int ClaimId { get; set; }
        public string ClientFirstName { get; set; }
        public string ClientLastName { get; set; }
        public DateTime ClaimFrom { get; set; }
        public DateTime ClaimThrough { get; set; }
        public string ClaimStatus { get; set; }
        public DateTime? BilledDate { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal Adjustments { get; set; }
        public decimal WriteOff { get; set; }
        public decimal PatientResponsibility { get; set; }
        public decimal AdjustedClaimAmount { get; set; }
        public decimal PaymentReceived { get; set; }
        public decimal NetReceivable { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
    }
}

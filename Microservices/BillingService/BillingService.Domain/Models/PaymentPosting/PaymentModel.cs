using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentModel
    {
        public int Id { get; set; }
        public string EraPaymentMethod { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public string FunderName { get; set; }
        public string FunderId { get; set; }
        public string Reference { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public int ClaimsCount { get; set; }
        public int DeniedClaimsCount { get; set; }
        public string ReconcileStatus { get; set; }
        public float PayedProcent { get; set; }
        public string PaymentIdentifier { get; set; }
        public string EraDocumentEdi { get; set; }
        public bool IsManual { get; set; }
        public PaymentTypes PaymentType { get; set; }
        public List<int> ClaimIds { get; set; }
        public string PaymentChannel { get; set; }   
        public string PaymentSource { get; set; }
        //todo add notes
    }
}
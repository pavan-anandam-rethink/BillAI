using System;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class ClientPrintData
    {
        public int PatientId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhones { get; set; }
        public string CompanyLogoUrl { get; set; }
        public string ClientName { get; set; }
        public string ClientAddress { get; set; }
        public DateTime PaymentPostingDate { get; set; }
        public string ClientAccountId { get; set; }
        public double TotalPayment { get; set; }
        public double Remaining { get; set; }
        public int ClaimId { get; set; }
        public string CompanyEmail { get; set; }
    }
}

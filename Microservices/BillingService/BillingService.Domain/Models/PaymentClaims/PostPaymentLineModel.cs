using System;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PostPaymentLineModel
    {
        public int Id { get; set; }
        public DateTime? DateOfService { get; set; }
        public string Procedure { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal PatientResponsibility { get; set; }
        public decimal? Balance { get; set; }
    }
}

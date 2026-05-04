using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class EOBPaymentInfo
    {
        public int Id { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string CheckNumber { get; set; }
        public DateTime? IssuedDate { get; set; }
        public DateTime? RecievedDate { get; set; }
        public string PayerName { get; set; }
        public string PayerLocation { get; set; }
        public string PayerPhoto { get; set; }
        public string PayerPhoneNumber { get; set; }
        public string PayerRoutingNumber { get; set; }
        public string PayerBankId { get; set; }
        public string PayerId { get; set; }
        public string PayeePhoto { get; set; }
        public string PayeeName { get; set; }
        public string PayeeLocation { get; set; }
        public string PayeeBankId { get; set; }
        public string PayeeRoutingNumber { get; set; }
        public string PayeeId { get; set; }
        public string PayeeAddress { get; set; }
        public int? AccountInfoId { get; set; }

        public PayeeAddress PayeeAdressObject { get; set; }
    }


    public class PayeeAddress
    {
        public string PayeeAddress1 { get; set; }
        public string PayeeAddress2 { get; set; }
        public string PayeeAddressCity { get; set; }
        public string PayeeAddressState { get; set; }
        public string PayeeAddressZip { get; set; }
        public string PayeeAddressCountry { get; set; }
    }
}

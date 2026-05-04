using System.Collections.Generic;

namespace BillingService.Domain.Models.PatientInvoice
{
    public class InvoiceRequestModel
    {
        public int AccountId { get; set; }
        public int ClientId { get; set; }
        public List<ChargeModel> Charges { get; set; }
    }

    public class PrintAndSubmitRequestModel
    {
        public List<InvoiceRequestModel> InvoiceRequests { get; set; }
        public bool includePreviousInvoices { get; set; }
    }

    public class ChargeModel
    {
        public int ChargeId { get; set; }
        public string BillingCode { get; set; }
        public decimal Units { get; set; }
        public string? DOS { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal InsurancePayments { get; set; }
        public decimal AdjustmentNonPatientResponsibility { get; set; }
        public decimal AdjustmentPatientResponsibility { get; set; }
        public decimal PatientPayments { get; set; }
        public decimal PatientBalance { get; set; }
    }
    public class PdfResponse
    {
        public string PdfBase64 { get; set; }
        public List<string> Errors { get; set; }
    }
}

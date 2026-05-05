using System.Collections.Generic;

namespace BillingService.Domain.Models.Clients.History
{
    public class InvoiceHistoryResponseModel
    {
        public int TotalCount { get; set; }
        public List<InvoiceHistoryResponse> Data { get; set; }
        
    }
    public class InvoiceHistoryResponse
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string BillingCode { get; set; }
        public string DateOfService { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal Adjustments { get; set; }
        public decimal? AdjustmentsPR { get; set; }
        public decimal InsurancePayments { get; set; }
        public decimal PatientPayments { get; set; }
        public decimal PatientBalance { get; set; }
        public string InvoiceNumber { get; set; }
        public string InvoiceDate { get; set; }
        public string PaymentDue { get; set; }
        public string Status { get; set; }
        public string PlaceOfService { get; set; }
        public string RenderingProvider { get; set; }
    }

}

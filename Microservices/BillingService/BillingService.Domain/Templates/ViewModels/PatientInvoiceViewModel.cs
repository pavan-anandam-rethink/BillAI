using System;
using System.Collections.Generic;

namespace BillingService.Domain.Templates.ViewModels
{
    public class PatientInvoiceViewModel
    {
        public BillingProviderInfo BillingProviderInfo { get; set; }
        public ClientInfo clientInfo { get; set; }
        public GuarantorInfo guarantorInfo {  get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDate { get; set; }
        public string? PaymentDue { get; set; }
        public List<BillingDetailViewModel> BillingDetails { get; set; }
        public string? Message { get; set; }
        public string? Remark { get; set; }
        public bool IsPreviousInvoice { get; set; }
    }
    public class BillingDetailViewModel
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string BillingCode { get; set; }
        public decimal Units { get; set; }
        public string? DateOfService { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal Adjustments { get; set; }
        public decimal? AdjustmentsPR { get; set; }
        public decimal InsurancePayments { get; set; }
        public decimal PatientPayments { get; set; }
        public decimal PatientBalance { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDate { get; set; }
        public string? PaymentDue { get; set; }
        public string Status { get; set; }
    }
    public class ClientInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Town { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string CustomerID { get; set; }
    }

    public class GuarantorInfo
    {
        public string? Name { get; set; }

        public string? Contact { get; set; }

        public string? Address { get; set; }
    }
    public class BillingProviderInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
    }

    public class InvoiceDetailsModel
    {
        public int Id { get; set; } // clientId
        public string ClientName { get; set; }
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal? TotalAdjustmentsPR { get; set; }
        public decimal TotalInsurancePayments { get; set; }
        public decimal TotalPatientPayments { get; set; }
        public decimal TotalPatientBalance { get; set; }

        public List<BillingDetailViewModel> BillingDetails { get; set; }
        public string GuarantorName { get; set; }
    }

    public class ChargeDetails
    {
        public int Id { get; set; }
        public string BillingCode { get; set; }
        public decimal Units { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal WriteOffAmount { get; set; }

    }

    public class ClientDetail
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
    }
}

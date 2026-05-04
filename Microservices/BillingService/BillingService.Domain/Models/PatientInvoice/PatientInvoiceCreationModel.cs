using System;

namespace BillingService.Domain.Models.PatientInvoice
{
    public class PatientInvoiceCreationModel
    {
        // charge ID
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string BillingCode { get; set; }
        public string? DateOfService { get; set; }
        public decimal Units { get; set; }
        public decimal Charges { get; set; }
        public decimal InsuranceAmount { get; set; }
        public decimal? Adjustment_Patient_responsibility { get; set; }
        public decimal Adjustment_Non_Patient_responsibility { get; set; }
        public decimal PatientAmount { get; set; }
        public decimal PatientBalance { get; set; }
        public string Invoicestatus { get; set; }
        public string GuarantorName { get; set; }
    }

    public class CreateInvoice
    {
        public string? ClientIds { get; set; } = string.Empty;
        public decimal? PatientResponsibilityFrom { get; set; } = null;
        public decimal? PatientResponsibilityTo { get; set; } = null;
        public DateTime? DateOfServiceFrom { get; set; } = null;
        public DateTime? DateOfServiceTo { get; set; } = null;

    }

    public class CreateInvoiceFilters : UserInfo
    {
        public CreateInvoice Filters { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }

    public class PendingCollectionFilters : UserInfo
    {
        public PendingCollection Filters { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }

    public class PendingCollection
    {
        public string? ClientIds { get; set; } = string.Empty;
        public decimal? PatientResponsibilityFrom { get; set; } = null;
        public decimal? PatientResponsibilityTo { get; set; } = null;
        public DateTime? DateOfServiceFrom { get; set; } = null;
        public DateTime? DateOfServiceTo { get; set; } = null;
        public DateTime? InvoiceFrom { get; set; } = null;
        public DateTime? InvoiceTo { get; set; } = null;
        public DateTime? PaymentDueFrom { get; set; } = null;
        public DateTime? PaymentDueTo { get; set; } = null;

    }

    public class BasicChargeDetails
    {
        public int? ChargeId { get; set; }
        public DateTime? DateOfService { get; set; }
        public int ClientId { get; set; }

    }
}

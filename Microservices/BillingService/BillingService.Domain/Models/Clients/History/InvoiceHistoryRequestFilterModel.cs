using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Clients.History
{
    public class InvoiceHistoryRequestFilterModel : UserInfo
    {
        public List<int> Status { get; set; }
        public decimal? PatientResponsibilityFrom { get; set; } = null;
        public decimal? PatientResponsibilityTo { get; set; } = null;
        public DateTime? DateOfServiceFrom { get; set; } = null;
        public DateTime? DateOfServiceTo { get; set; } = null;
        public DateTime? InvoiceDateFrom { get; set; } = null;
        public DateTime? InvoiceDateTo { get; set; } = null;
        public DateTime? InvoiceDueDateFrom { get; set; } = null;
        public DateTime? InvoiceDueDateTo { get; set; } = null;
        public decimal? PatientBalanceFrom { get; set; } = null;
        public decimal? PatientBalanceTo { get; set; } = null;




    }
}
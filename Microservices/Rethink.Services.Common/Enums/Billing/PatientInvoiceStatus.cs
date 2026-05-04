using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum PatientInvoiceStatus
    {
        [Description("Ready To Invoice")]
        ReadytoInvoice = 1,
        [Description("Invoice Sent")]
        InvoiceSent = 2,
        [Description("Partially Paid")]
        PartiallyPaid = 3,
        [Description("Fully Paid")]
        FullyPaid = 4
    }
}

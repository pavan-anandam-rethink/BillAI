using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models
{
    public class AdjustmentReasonCodes
    {
        public string ReasonCode { get; set; }
        public string Description { get; set; }
        public bool? IsDefault { get; set; }
    }
}

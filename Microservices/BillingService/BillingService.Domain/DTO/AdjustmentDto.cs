using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.DTO
{
    public class AdjustmentDto
    {
        public int PaymentClaimServiceLineId { get; set; }
        public decimal? AdjustmentAmount { get; set; }
        public bool? IsAdjustmentPositive { get; set; }
        public string AdjustmentGroupCode { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.DTO
{
    public class ErrorDto
    {
        public int PaymentClaimServiceLineId { get; set; }
        public int?  ErrorType { get; set; }
    }
}

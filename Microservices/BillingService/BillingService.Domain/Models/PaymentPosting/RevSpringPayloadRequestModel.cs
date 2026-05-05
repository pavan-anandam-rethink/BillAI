using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class RevSpringPayloadRequestModel
    {
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
        public int ClientId { get; set; }
        public decimal AmountDue { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string UserEmail { get; set; }
        public string UserLastName { get; set; }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients
{
    public class ClientRecordFilterModel
    {
        public List<int>? LocationId { get; set; }
        public List<int>? ClientId { get; set; }
        public List<int>? FunderId { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

}

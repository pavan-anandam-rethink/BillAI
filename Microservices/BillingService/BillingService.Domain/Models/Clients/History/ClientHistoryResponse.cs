using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients.History
{
    public class ClientHistoryResponseModel
    {
        public List<ClientHistoryResponse> clientHistoryResponse { get; set; }
        public int Total { get; set; }
    }
    public class ClientHistoryResponse
    {
        public string ClientName { get; set; }
        public string ClientId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Address { get; set; }
        public decimal Billed { get; set; }
        public decimal InsurancePaid { get; set; }
        public decimal PatientPaid { get; set; }
        public decimal RemainingClaimBalance { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Location { get; set; }
        public string PrimaryFunder { get; set; }
        public string SecondaryFunder { get; set; }
    }

}

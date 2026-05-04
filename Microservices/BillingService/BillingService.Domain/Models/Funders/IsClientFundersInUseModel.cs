using System.Collections.Generic;

namespace BillingService.Domain.Models.Funders
{
    public class IsClientFundersInUseModel
    {
        public List<int> ClientFunderIds { get; set; }
        public int ClientId { get; set; }
    }

    public class ClientFunderWithClaimModel
    {
        public int ClientFunderId { get; set; }
        public bool HasClaim { get; set; }
    }
}

using System.Collections.Generic;

namespace BillingService.Domain.Models.Funders
{
    public class FunderDropdownResponseModel
    {
        public List<FunderDropdownModel> Funders { get; set; }
        public int TotalCount { get; set; }
    }
}
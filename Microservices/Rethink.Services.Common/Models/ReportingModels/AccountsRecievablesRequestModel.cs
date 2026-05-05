using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class AccountsRecievablesRequestModel
    {
        public List<int> PayerOrFunder { get; set; }
        public DateTime closingDate { get; set; }
        public int AccountInfoId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<SortingModel> SortingModels { get; set; }
    }
}

using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.BillingSettings
{
    public class BillingFunderListRequestModel : UserInfo
    {
        public List<SortingModel> SortingModels { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<GridFilterModel> FilterModels { get; set; }
    }
}

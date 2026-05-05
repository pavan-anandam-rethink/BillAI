using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClearingHouseDataModel
    {
        public string title { get; set; }
        public int connectionTypeId { get; set; }
        public string urlLink { get; set; }
        public string userName { get; set; }
        public string userPassword { get; set; }
        public string notes { get; set; }
        public bool isDefault { get; set; }
        public string taxId { get; set; }
        public int clearingHouseTypeId { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }

    }

    [Owned]
    public class ClearingHouseModel
    {
        public int Total { get; set; }
        public List<ClearingHouseDataModel> Data { get; set; }
    }
}

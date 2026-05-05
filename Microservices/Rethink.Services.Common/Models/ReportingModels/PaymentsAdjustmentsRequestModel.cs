using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class PaymentsAdjustmentsRequestModel
    {
        public List<SortingModel> SortingModels { get; set; }
        public List<int> FunderId { get; set; }
        public int AccountInfoId { get; set; }
        public int RangeType { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsExport { get; set; } = false;
    }
}

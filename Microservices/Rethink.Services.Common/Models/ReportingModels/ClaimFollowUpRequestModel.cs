using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ClaimFollowUpRequestModel
    {
        public List<SortingModel> SortingModels { get; set; }
        public List<int> FunderIds { get; set; }
        public int AccountInfoId { get; set; }
        public int FollowUpType { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class ClaimFollowUpResponseModel
    {
        public List<ClaimFollowUpResponse> claimFollowUps { get; set; }
        public int totalCount { get; set; }
    }
}

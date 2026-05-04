using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.Claim
{
    public class ClaimFlagReasonModel
    {
        public int Id { get; set; }
        public string ReasonName { get; set; } = null!;
        public string? ReasonDescription { get; set; }
        public int AccountInfoId { get; set; }
    }

}

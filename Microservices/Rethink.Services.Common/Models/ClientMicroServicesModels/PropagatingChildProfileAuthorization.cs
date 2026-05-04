using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class PropagatingChildProfileAuthorization
    {
        public int id { get; set; }
        public MetaData? metaData { get; set; }
        public string authorizationNumber { get; set; }
        public int childProfileDiagnosisId { get; set; }
        public int authorizationRenderingProviderTypeId { get; set; }
        public int? renderingProviderStaffId { get; set; }
        public int? noOfUnits { get; set; }
        public int schedulingGoalNoOfUnits { get; set; }
        public int diagnosisLUId { get; set; }
        public DateTime endDate { get; set; }
        public int childProfileAuthorizationId { get; set; }
        public int childProfileReferringProviderId { get; set; }
    }
}

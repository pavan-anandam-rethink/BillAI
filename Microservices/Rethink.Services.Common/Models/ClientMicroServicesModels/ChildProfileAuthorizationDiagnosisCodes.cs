using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ChildProfileAuthorizationDiagnosisCode
    {
        public int id { get; set; }
        public MetaData? metaData { get; set; }
        public int childProfileAuthorizationId { get; set; }
        public int diagnosisId { get; set; }
        public int order { get; set; }
        public bool includeOnClaims { get; set; }
        public int childProfileDiagnosisId { get; set; }
        public ClientAuthorization ChildProfileAuthorization { get; set; }
        public Diagnosis Diagnosis { get; set; }
    }
}

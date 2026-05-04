using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class InsuranceContactsTypeModel
    {
        public int? relationshipToInsuredTypeId { get; set; }
        public int? insuranceTypeId { get; set; }
        public string? medicalRecordNumber { get; set; }
        public int copaymentTypeId { get; set; }
        public string insurancePolicyNumber { get; set; }
        public string insuranceGroupNumber { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}

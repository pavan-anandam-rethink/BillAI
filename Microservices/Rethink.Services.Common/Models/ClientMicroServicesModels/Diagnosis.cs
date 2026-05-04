using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Enums.BH;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class Diagnosis
    {
        public int? accountInfoId { get; set; }
        public string name { get; set; }
        public int pos { get; set; }
        public string diagnosisCode { get; set; }
        public string description { get; set; }
        public DiagnosisTypes diagnosisTypeId { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}

using Rethink.Services.Common.Enums.BH;
using System;

namespace Rethink.Services.Common.Models.RethinkDataEntityClasses
{
    public class DiagnosisEntityModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Pos { get; set; }
        public string? DiagnosisCode { get; set; }
        public DiagnosisTypes TypeId { get; set; }
        public string? Description { get; set; }
        public int? AccountInfoId { get; set; }
        public bool IncludeOnClaims { get; set; }
        public int Order { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}

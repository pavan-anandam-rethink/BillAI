using System;

namespace Rethink.Services.Common.Models
{
    public class MetaData
    {
        public int Id { get; set; }
        public DateTime createdOn { get; set; }
        public int createdBy { get; set; }
        public DateTime? modifiedOn { get; set; }
        public int? modifiedBy { get; set; }
        public DateTime? deletedOn { get; set; }
        public int? deletedBy { get; set; }
    }
}

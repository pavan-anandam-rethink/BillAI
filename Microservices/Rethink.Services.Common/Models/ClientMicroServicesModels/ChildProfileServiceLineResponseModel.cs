using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Enums.BH;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ChildProfileServiceLineResponseModel
    {
        public int total { get; set; }
        public List<ServiceLines> data { get; set; }
    }
    [Owned]
    public class ServiceLines
    {
        public int serviceId { get; set; }
        public int ChildProfileFunderMappingId { get; set; }
        public int id { get; set; }
        public ResponsibilitySequenceType responsibilitySequence { get; set; } = ResponsibilitySequenceType.Primary;
        public FunderDetails ChildProfileFunderMapping { get; set; }
        public MetaData metaData { get; set; }
    }
    [Owned]
    public class ChildProfileServiceLines
    {
        public int accountId { get; set; }
        public string name { get; set; }
        public bool isActive { get; set; }
        public string description { get; set; }
        public string isDph { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}

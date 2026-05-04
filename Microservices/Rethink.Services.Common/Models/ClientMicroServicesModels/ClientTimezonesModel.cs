using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientTimezonesModel
    {
        public string name { get; set; }
        public string displayName { get; set; }
        public string simpleName { get; set; }
        public int displayOrder { get; set; }
        public int? sandataTimezoneId { get; set; }
        public int id { get; set; }
        public MetaData? metaData { get; set; }
    }
}

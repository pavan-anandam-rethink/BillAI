using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class LocationCodesModel
    {
        public string description { get; set; }
        public string code { get; set; }
        public int orderBy { get; set; }
        public bool isDph { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}

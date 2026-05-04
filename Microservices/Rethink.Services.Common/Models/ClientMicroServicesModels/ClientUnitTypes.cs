using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientUnitTypes
    {
        public int? unit { get; set; }
        public string unitString { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}

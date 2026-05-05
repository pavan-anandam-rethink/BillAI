using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientProviderServiceModel
    {
        public int accountId { get; set; }
        public string name { get; set; }
        public decimal baseRate { get; set; }
        public bool isActive { get; set; }
        public int id { get; set; }
        public MetaData? metaData { get; set; }
    }
}

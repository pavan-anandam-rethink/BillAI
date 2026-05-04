using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClientReasonCodes
    {
        public string name { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}

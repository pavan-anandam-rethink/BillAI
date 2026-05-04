using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientAddress
    {
        public int Id { get; set; }
        public string? street1 { get; set; }
        public string? street2 { get; set; }
        public string? city { get; set; }
        public int? stateId { get; set; }
        public string? state { get; set; } = string.Empty;
        public string? country { get; set; } = string.Empty;
        public string? zipCode { get; set; }
        public string? zip { get; set; }
        public int? countryId { get; set; }
        public string? town { get; set; }
        public MetaData? metaData { get; set; }
    }

}

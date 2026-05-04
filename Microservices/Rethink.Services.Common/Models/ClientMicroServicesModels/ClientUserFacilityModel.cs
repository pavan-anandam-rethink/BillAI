using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientUserFacilityModel
    {
        public int providerLocationId { get; set; }
    }
}

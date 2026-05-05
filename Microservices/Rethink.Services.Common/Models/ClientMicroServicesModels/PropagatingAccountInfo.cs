using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class PropagatingAccountInfo
    {
        public string name { get; set; }
    }
}

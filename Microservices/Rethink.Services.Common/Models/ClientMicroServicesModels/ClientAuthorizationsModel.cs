using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClientAuthorizationsModel
    {
        public int total { get; set; }
        public List<ClientAuthorization> data { get; set; }
    }


}

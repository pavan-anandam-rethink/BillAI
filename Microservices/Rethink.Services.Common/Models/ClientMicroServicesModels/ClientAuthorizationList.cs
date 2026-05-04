using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    public class ClientAuthorizationList
    {
        public int total { get; set; }
        public List<ClientAuthorization> data { get; set; }
    }
}

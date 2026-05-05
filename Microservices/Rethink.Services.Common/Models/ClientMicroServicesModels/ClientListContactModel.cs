using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClientListContactModel
    {
        public int total { get; set; }
        public List<ClientContactsModel> data { get; set; }
    }
}

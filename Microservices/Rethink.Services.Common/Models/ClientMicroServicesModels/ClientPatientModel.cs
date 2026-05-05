using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    public class ClientPatientModel
    {
        public int Total { get; set; }
        public List<ClientUserModel> Data { get; set; }
    }
}

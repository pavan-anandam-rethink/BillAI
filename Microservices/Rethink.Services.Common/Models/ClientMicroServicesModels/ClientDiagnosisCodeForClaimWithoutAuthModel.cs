using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClientDiagnosisCodeForClaimWithoutAut
    {
        public int total { get; set; }
        public List<ClientDiagnosisCodes> data { get; set; }
    }
}

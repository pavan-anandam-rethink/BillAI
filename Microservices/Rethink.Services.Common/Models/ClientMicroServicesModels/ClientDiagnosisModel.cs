using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    public class ClientRethinkDiagnosisModel
    {
        public int total { get; set; }
        public List<Diagnosis> data { get; set; }
    }
    [Owned]
    public class ClientDiagnosisModel
    {
        public int diagnosisId { get; set; }
        public string diagnosisCode { get; set; }
        public string description { get; set; }
        public string diagnosisFullDescription { get; set; }
        public int order { get; set; }
        public int id { get; set; }
        public bool includeOnClaims { get; set; }
    }
}

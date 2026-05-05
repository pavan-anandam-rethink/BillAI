using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    [Owned]
    public class ClientDiagnosisCodes
    {
        public int id { get; set; }
        public int clientId { get; set; }
        public int? diagnosisId { get; set; }
        public string physician { get; set; }
        public string diagnosisDescription { get; set; }
        public string npiNumber { get; set; }
        public string? physicianAddress { get; set; }
        public bool includeOnClaims { get; set; }
        public string physicianCredential { get; set; }
        public DateTime? endDate { get; set; }
        public int serviceLineId { get; set; }
        public int order { get; set; }
        public Diagnosis diagnosis { get; set; }
        public MetaData metaData { get; set; }
    }
}

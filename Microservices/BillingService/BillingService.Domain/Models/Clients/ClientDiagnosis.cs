namespace BillingService.Domain.Models.Clients
{
    public class ClientDiagnosis
    {
        public int Id { get; set; }
        public int ChildProfileId { get; set; }
        public bool CanDelete { get; set; }
        public int DiagnosisId { get; set; }
        public string DiagnosisDescription { get; set; }
        public string DiagnosisLUCode { get; set; }
        public string DiagnosisLUDescription { get; set; }
        public string DiagnosisInfo { get; set; }
        public string Physician { get; set; }
        public string NPINumber { get; set; }
        public int? AccountInfoId { get; set; }
        public bool IsCustom { get; set; }
        public int DiagnosisLUTypeId { get; set; }
        public string PhysicianAddress { get; set; }
        public string PhysicianCredential { get; set; }
    }
}

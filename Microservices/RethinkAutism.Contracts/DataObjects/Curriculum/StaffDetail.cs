namespace RethinkAutism.Contracts.DataObjects.Curriculum
{
    public class StaffDetail
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string StaffInitials { get; set; }
        public string StaffTitle { get; set; }
        public string StaffLocation { get; set; }
        public string StaffSupervisorName { get; set; }
        public bool? IsParentVerificationRequired { get; set; }
        public bool? IsSessionNoteEnteredRequired { get; set; }
    }
}

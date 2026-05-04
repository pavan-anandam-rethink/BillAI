using System;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimNote
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public DateTime RemindDate { get; set; }
        public string Note { get; set; }
        public bool RecievedReminder { get; set; }


        public int CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public string CreatedByName { get; set; }
        public string ModifiedByName { get; set; }
    }

    public class ClaimNoteSmall
    {
        public int ClaimId { get; set; }
        public DateTime RemindDate { get; set; }
        public string Note { get; set; }
    }

    public class ClaimNoteSaveModel : UserInfo
    {
        public int ClaimId { get; set; }
        public DateTime RemindDate { get; set; }
        public string Note { get; set; }
    }

    public class ClaimNoteDeleteModel : UserInfo
    {
        public int Id { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class ClaimNoteGetAllModel : UserInfo
    {
        public int Id { get; set; }
    }


    public class ClaimNoteRequestModel : UserInfo
    {
        public ClaimNoteSmall[] ClaimNoteModels { get; set; }
    }
}

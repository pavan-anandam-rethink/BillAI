using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentNote
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
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


    public class PaymentNoteSmall
    {
        public int PaymentId { get; set; }
        public DateTime RemindDate { get; set; }
        public string Note { get; set; }
    }

    public class PaymentNoteSaveModel : UserInfo
    {
        public int PaymentId { get; set; }
        public DateTime RemindDate { get; set; }
        public string Note { get; set; }
    }

    public class PaymentNoteDeleteModel : UserInfo
    {
        public int Id { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class PaymentNoteRequestModel
    {
        public PaymentNoteSmall[] PaymentNoteModels { get; set; }
        public int MemberId { get; set; }
    }
}

using System;

namespace BillingService.Domain.Models
{
    public class ChargeNoteModel
    {
        public string NoteText { get; set; }
        public string NoteCreatorName { get; set; }
        public DateTime NoteCreatedDate { get; set; }
    }
}

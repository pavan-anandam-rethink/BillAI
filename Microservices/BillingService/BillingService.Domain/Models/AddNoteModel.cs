namespace BillingService.Domain.Models
{
    public class AddNoteModel
    {
        public int ChargeId { get; set; }
        public string? NoteText { get; set; }
        public int? NoteCreatedBy { get; set; }
    }
}

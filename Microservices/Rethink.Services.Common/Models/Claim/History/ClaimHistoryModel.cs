using Rethink.Services.Common.Enums.Billing;
using System;

namespace Rethink.Services.Common.Models.Claim.History
{
    public class ClaimHistoryModel
    {
        public DateTime ChangeDate { get; set; }
        public string ChangeBy { get; set; }
        public ClaimAction ActionId { get; set; }
        public ClaimHistoryAction HistoryActionId { get; set; }
        public ClaimActionMode Mode { get; set; }
        public int? FieldId { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public int? ClaimVersionHistoryId { get; set; }
        public ClaimResponseFileType ResponseFileTypeId { get; set; }
        public string BatchId { get; set; }
        public string RethinkUser { get; set; } = "N/A";
    }
}
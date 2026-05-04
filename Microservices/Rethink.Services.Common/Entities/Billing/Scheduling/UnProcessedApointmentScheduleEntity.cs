using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;

namespace Rethink.Services.Common.Entities.Billing.Scheduling;

public class UnProcessedApointmentScheduleEntity : BasePersistEntity
{
    public int AppointmentId { get; set; }
    public int AccountInfoId { get; set; }
    public int FunderId { get; set; }
    public string ClaimCreationFrequency { get; set; }
    public string SelectedDays { get; set; }
    public int? Frequency { get; set; }
    public string ExecutionTime { get; set; }
    public DateTime UtcExecutionDateTime { get; set; }
    public string TimeZone { get; set; }
    public string ProcessingStatus { get; set; } = ProcessingState.Unprocessed.ToString();
    public string CreatedBy { get; set; } = "System";
    public int? ModifiedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public DateTime? DeletedOn { get; set; }
    public int Retry { get; set; } = 0;
}
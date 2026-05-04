using Rethink.Services.Common.Enums.Billing;
using System;

namespace BillingService.Domain.Scheduling.Models;

public class UnProcessedApointmentSchedule
{
    public int AppointmentId { get; set; }
    public int AccountInfoId { get; set; }
    public int FunderId { get; set; }
    public ClaimCreationFrequency ClaimCreationFrequency { get; set; }
    public string SelectedDays { get; set; }
    public int? Frequency { get; set; }
    public string ExecutionTime { get; set; }
    public TimeSpan UtcExecutionTime { get; set; }
    public DateTime CurrentUtc { get; set; }
    public string TimeZone { get; set; }
}
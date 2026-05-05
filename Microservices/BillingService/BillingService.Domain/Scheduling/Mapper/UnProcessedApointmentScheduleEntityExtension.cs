using BillingService.Domain.Scheduling.Models;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using System;

namespace BillingService.Domain.Scheduling.Mapper;

public static class UnProcessedApointmentScheduleEntityExtension
{
    public static UnProcessedApointmentScheduleEntity ToUnProcessedApointmentScheduleEntity(this UnProcessedApointmentSchedule source, DateTime nextExecution)
    {
        if (source is null)
            return null;

        return new UnProcessedApointmentScheduleEntity
        {
            AccountInfoId = source.AccountInfoId,
            FunderId = source.FunderId,
            AppointmentId = source.AppointmentId,
            ClaimCreationFrequency = source.ClaimCreationFrequency.ToString(),
            SelectedDays = source.SelectedDays,
            Frequency = source.Frequency,
            ExecutionTime = source.ExecutionTime,
            UtcExecutionDateTime = nextExecution.TrimToMilliseconds(),
            TimeZone = source.TimeZone,
            CreatedOn = DateTime.UtcNow.TrimToMilliseconds()
        };
    }
}
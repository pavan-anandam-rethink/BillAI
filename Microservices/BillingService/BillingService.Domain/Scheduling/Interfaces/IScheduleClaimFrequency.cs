using BillingService.Domain.Scheduling.Models;
using System;

namespace BillingService.Domain.Scheduling.Interfaces;

public interface IScheduleClaimFrequency
{
    DateTime GetNextExecutionUtc(UnProcessedApointmentSchedule unProcessedApointmentSchedule);
}
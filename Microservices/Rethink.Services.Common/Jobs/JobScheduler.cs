using System;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Jobs
{
    [ExcludeFromCodeCoverage]
    public class JobScheduler
    {
        public Type JobType { get; }
        public string CronExpression { get; }

        public JobScheduler(Type jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }
    }
}
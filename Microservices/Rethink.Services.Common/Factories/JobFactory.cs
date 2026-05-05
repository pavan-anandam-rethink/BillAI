using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Factories
{
    [ExcludeFromCodeCoverage]
    public class JobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
        }


        public void ReturnJob(IJob job)
        {
            Debug.WriteLine($"Destroying job: {job?.GetType().Name}");
        }
    }
}
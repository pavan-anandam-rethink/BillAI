using Microsoft.Extensions.Logging;
using Quartz;

namespace Rethink.Services.Common.Jobs
{
    public interface IScheduledJob
    {
        IJobDetail JobDetail { get; }
        ITrigger Trigger { get; }

        bool IsActive { get; }

        void Build(ILogger logger);
    }
}

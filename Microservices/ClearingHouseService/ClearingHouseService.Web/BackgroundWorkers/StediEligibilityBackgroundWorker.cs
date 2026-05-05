using ClearingHouseService.Web.Service.Handler;
using Rethink.Services.Common.Dtos.ClearingHouse;
using Rethink.Services.Common.Interfaces;

namespace ClearingHouseService.Web.BackgroundWorker
{
    public sealed class StediEligibilityBackgroundWorker : BackgroundService
    {
        private readonly IBackgroundJobQueue _backgroundJobQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StediEligibilityBackgroundWorker> _logger;

        public StediEligibilityBackgroundWorker(
            IBackgroundJobQueue queue,
            IServiceProvider serviceProvider,
            ILogger<StediEligibilityBackgroundWorker> logger)
        {
            _backgroundJobQueue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);

                    if (job is StediEligibilityJobDTO stediJob)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<StediEligibilityJobHandler>();

                        await handler.HandleAsync(stediJob, stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("StediEligibilityBackgroundWorker job type received: {JobType}", job.GetType().Name);
                    }
                }              
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing StediEligibilityBackgroundWorker job");
                }
            }
        }
    }

}
using ClearingHouseService.Web.Interface;
using Rethink.Services.Common.Dtos.ClearingHouse;

namespace ClearingHouseService.Web.Service.Handler
{
    public class StediEligibilityJobHandler
    {
        private readonly IStediEligibilityProcessor _processor;
        private readonly ILogger<StediEligibilityJobHandler> _logger;


        public StediEligibilityJobHandler(
       IStediEligibilityProcessor processor,
       ILogger<StediEligibilityJobHandler> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        public async Task HandleAsync(StediEligibilityJobDTO stediEligibilityJob, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Calling STEDI eligibilityHandler. CorrelationId={CorrelationId},FunderId={FunderId},MemberId={MemberId}", stediEligibilityJob.CorrelationId, stediEligibilityJob.FunderId, stediEligibilityJob.MemberId);

            await _processor.ProcessAsync(stediEligibilityJob, cancellationToken);

            _logger.LogInformation("STEDI eligibility job completed. CorrelationId={CorrelationId},FunderId= {FunderId},MemberId ={MemberId}", stediEligibilityJob.CorrelationId, stediEligibilityJob.FunderId, stediEligibilityJob.MemberId);

        }
    }
}

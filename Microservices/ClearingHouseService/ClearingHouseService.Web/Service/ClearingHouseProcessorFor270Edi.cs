using ClearingHouseService.Web.Interface;
using Rethink.Services.Common.Models.EligibilityRequest;

namespace ClearingHouseService.Web.Service
{
    public class ClearingHouseProcessorFor270Edi : IClearingHouseProcessorFor270Edi
    {
        private readonly ICommon _commonService;
        private readonly ILogger<ClearingHouseProcessorFor270Edi> _logger;
        public ClearingHouseProcessorFor270Edi(ICommon commonservice, ILogger<ClearingHouseProcessorFor270Edi> logger)
        {
            _commonService = commonservice;
            _logger = logger;
        }
        public async Task<(bool success, string result)> Generate270EDIData(Eligibility270Request eligibility270Request)
        {
            try
            {
                _logger.LogInformation("Generating EDI for FunderId: {eligibility270Request.FunderId}, ClearingHouseId: {eligibility270Request.ClearingHouseId}", eligibility270Request.FunderId, eligibility270Request.ClearingHouseId);
                return await _commonService.Generate270EDIData(eligibility270Request);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating EDI for FunderId: {eligibility270Request.FunderId}, ClearingHouseId: {eligibility270Request.ClearingHouseId}", eligibility270Request.FunderId, eligibility270Request.ClearingHouseId);
                return (false, ex.Message.ToString());
            }
        }
    }
}

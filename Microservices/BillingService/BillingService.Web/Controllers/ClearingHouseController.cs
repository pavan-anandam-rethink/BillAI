using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.EligibilityRequest;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{

    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ClearingHouseController : ControllerBase
    {
        private readonly ICHService _clearingHouseService;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IPaymentPostingService _paymentPostingService;
        private readonly ILogger<ClearingHouseController> _logger;
        private readonly IMapper _mapper;


        public ClearingHouseController(ICHService clearingHouseService,
           IClaimManagerService claimManagerService,
           IPaymentPostingService paymentPostingService,
           ILogger<ClearingHouseController> logger,
           IMapper mapper
           )
        {
            _clearingHouseService = clearingHouseService;
            _claimManagerService = claimManagerService;
            _logger = logger;
            _paymentPostingService = paymentPostingService;
            _mapper = mapper;
        }


        [HttpPost]
        public async Task<IActionResult> GenerateEDIData([FromBody] ClearingHouseClaimModel claimModelDto)
        {
            _logger.LogInformation("{Controller}.{Action} - GenerateEDIData called. ClaimId={ClaimId}",
                nameof(ClearingHouseController),
                nameof(GenerateEDIData),
                claimModelDto?.claimId);

            try
            {
                var result = await _claimManagerService.GenerateEdi(claimModelDto);
                if (IsEdiData(result))
                {
                    return Ok(result);
                }
                else
                {
                    _logger.LogError($"{nameof(ClearingHouseController)}.{nameof(GenerateEDIData)} - GenerateEdi returned invalid data. ClaimId={claimModelDto?.claimId}");
                    return BadRequest("Failed to Generate EDI data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClearingHouseController)}.{nameof(GenerateEDIData)} - GenerateEDIData failed. ClaimId={claimModelDto?.claimId}, ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message.ToString());
            }

        }

        [HttpPost]
        public async Task<IActionResult> Generate270EDIData([FromBody] Eligibility270Request eligibilityRequest)
        {
            _logger.LogInformation("Generate270EDI data called. SubscriberId={SubscriberId}", eligibilityRequest.SubscriberId);

            var billingEligibilityDTO = _mapper.Map<Eligibility270DTO>(eligibilityRequest);
            var result = await _claimManagerService.Generate270Edi(billingEligibilityDTO);

           
            if (IsEdiData(result))
            {
                _logger.LogInformation("{Controller}.{Action} - Generated 270EDI data {@Eligibility270Request}", nameof(ClearingHouseController), nameof(Generate270EDIData), result);
                return Ok(result);
            }
            else
            {
                _logger.LogInformation("Failed to generated 270EDI data");
                return BadRequest("Failed to Generate 270 EDI data");
            }               
        }

        private bool IsEdiData(string data)
        {
            return data.StartsWith("ISA");
        }

        [HttpPost]
        public async Task<bool> UploadFileToBlobStorage([FromBody] ClaimUploadModelWithUserInfo filesWithUserInfo)
        {
            try
            {
                _logger.LogInformation(
                    "Uploading EDI file to blob storage. ClaimId={ClaimId}, FileName={FileName}",
                    filesWithUserInfo.ClaimId,
                    filesWithUserInfo.FileName);

                var result = await _clearingHouseService.UploadFileAsync(filesWithUserInfo);

                _logger.LogInformation(
                    "Successfully uploaded EDI file to blob storage. ClaimId={ClaimId}, FileName={FileName}",
                    filesWithUserInfo.ClaimId,
                    filesWithUserInfo.FileName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error at uploading EDI file to Azure blob storage:" +
                    $"ClaimId: {filesWithUserInfo.ClaimId}" +
                    $"FileName: {filesWithUserInfo.FileName}" +
                    $"ErrorMsg: {ex.Message}");

                return false;
            }

        }

        [HttpPost]
        public async Task<bool> UploadERAErrorFileToBlobStorage([FromBody] ERAUploadModel model)
        {
            try
            {
                _logger.LogInformation(
                        "Uploading ERA error file to blob storage. PaymentIds={PaymentIds}, FileName={FileName}",
                        string.Join(",", model.PaymentIds),
                        model.fileName);



                var result = await _clearingHouseService.UploadERAErrorFileAsync(model);
                _logger.LogInformation(
                        "Successfully uploaded ERA error file to blob storage. PaymentIds={PaymentIds}, FileName={FileName}",
                        string.Join(",", model.PaymentIds),
                        model.fileName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error at uploading EDI file to Azure blob storage:" +
                    $"PaymentIds: {string.Join(",", model.PaymentIds)}" +
                    $"fileName: {model.fileName}" +
                    $"ErrorMsg: {ex.Message}");

                return false;
            }

        }

        [HttpPost]
        public async Task<bool> UploadEDIResponseFile([FromBody] DownloadSftpDataModel fileStreams)
        {
            try
            {
                _logger.LogInformation("Uploading EDI response file(s) from SFTP to blob storage.");
                var result = await _clearingHouseService.UploadEDIResponseFile(fileStreams);

                _logger.LogInformation("Successfully uploaded EDI response file(s) from SFTP to blob storage.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error at uploading SFTP files to Azure blob storage:" +
                   $"Error: {ex.Message}");
                return false;
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateERAErrorData([FromBody] ERAUploadModel eRAPaymentIdsModel)
        {
            _logger.LogInformation("{Controller}.{Action} - GenerateERAErrorData called.",
                nameof(ClearingHouseController),
                nameof(GenerateERAErrorData));

            try
            {
                var result = await _paymentPostingService.GetERAErrors(eRAPaymentIdsModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClearingHouseController)}.{nameof(GenerateERAErrorData)} - GenerateERAErrorData failed. ErrorMsg ={ex.Message}");
                return BadRequest(ex.Message.ToString());
            }
        }
    }
}

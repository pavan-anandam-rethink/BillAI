using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BillingService.Web.Controllers
{
    [Area("Billing")]
    [Route("[controller]/[action]")]
    public class ClaimAttachmentController : Controller
    {
        private readonly IClaimAttachmentService _claimAttachmentService;

        private readonly IMapper _mapper;
        private readonly ILogger<ClaimAttachmentController> _logger;

        public ClaimAttachmentController(
            IClaimAttachmentService claimAttachmentService,
            IMapper mapper,
            ILogger<ClaimAttachmentController> logger)
        {
            _claimAttachmentService = claimAttachmentService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Delete(ClaimAttachmentModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - Delete called. MemberId={MemberId}, AccountInfoId={AccountInfoId}",
                    nameof(ClaimAttachmentController),
                    nameof(Delete),
                    model.MemberId,
                    model.AccountInfoId);

            try
            {
                var item = _mapper.Map<ClaimAttachmentItem>(model.ClaimAttachmentModel);
                item = await _claimAttachmentService.Delete(item, model.MemberId,
                    model.AccountInfoId);

                return Json(_mapper.Map<ClaimAttachmentModel>(item));
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimAttachmentController)}.{nameof(Delete)} - Delete failed. MemberId={model.MemberId}, " +
                    $"AccountInfoId={model.AccountInfoId}, ClaimAttachmentId={model.ClaimAttachmentModel?.Id}, Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromBody] ClaimUploadModelWithUserInfo filesWithUserInfo)
        {
            _logger.LogInformation("{Controller}.{Action} - UploadFile called. MemberId={MemberId}, ClaimId={ClaimId}, FileName={FileName}",
                    nameof(ClaimAttachmentController),
                    nameof(UploadFile),
                    filesWithUserInfo.MemberId,
                    filesWithUserInfo.ClaimId,
                    filesWithUserInfo.FileName);

            try
            {
                var result = await _claimAttachmentService.UploadFileAsync(filesWithUserInfo);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimAttachmentController)}.{nameof(UploadFile)} - UploadFile failed. " +
                    $"AccountInfoId={filesWithUserInfo.AccountInfoId}, ClaimId={filesWithUserInfo.ClaimId}, " +
                    $"FileName={filesWithUserInfo.FileName}, FileMimeType={filesWithUserInfo.FileMimeType}, Error: {ex.Message}");

                return BadRequest(ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> GetForClaim([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetForClaim called. MemberId={MemberId}, ClaimId={ClaimId}",
                    nameof(ClaimAttachmentController),
                    nameof(GetForClaim),
                    model.MemberId,
                    model.Id);

            try
            {
                var result = await _claimAttachmentService.GetForClaimAsync(model);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimAttachmentController)}.{nameof(GetForClaim)} - GetForClaim failed. MemberId={model.MemberId}, " +
                    $"ClaimId={model.Id}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> RenameAttachment([FromBody] RenameAttachmentModelWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - RenameAttachment called. MemberId={MemberId}, AttachmentId={AttachmentId}",
                    nameof(ClaimAttachmentController),
                    nameof(RenameAttachment),
                    model.MemberId,
                    model.AttachmentId);

            try
            {
                await _claimAttachmentService.RenameAttachmentAsync(model);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimAttachmentController)}.{nameof(RenameAttachment)} - RenameAttachment failed. MemberId={model.MemberId}, " +
                    $"AttachmentId={model.AttachmentId}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteUpload([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - DeleteUpload called. MemberId={MemberId}, UploadId={UploadId}",
                    nameof(ClaimAttachmentController),
                    nameof(DeleteUpload),
                    model.MemberId,
                    model.Id);

            try
            {
                await _claimAttachmentService.DeleteUpload(model);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimAttachmentController)}.{nameof(DeleteUpload)} - DeleteUpload failed. MemberId={model.MemberId}, UploadId={model.Id}, ErrorMsg={ex.Message}");
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetFileUpload([FromBody] IdWithUserInfo model)
        {
            _logger.LogInformation("{Controller}.{Action} - GetFileUpload called. MemberId={MemberId}, UploadId={UploadId}",
                    nameof(ClaimAttachmentController),
                    nameof(GetFileUpload),
                    model.MemberId,
                    model.Id);

            try
            {
                var result = await _claimAttachmentService.GetUploadAsync(model);

                return Ok(new { DownloadUrl = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ClaimAttachmentController)}.{nameof(GetFileUpload)} - GetFileUpload failed. MemberId={model.MemberId}, " +
                    $"UploadId={model.Id}, ErrorMsg={ex.Message}");

                return BadRequest(ex.Message);
            }
        }
    }
}
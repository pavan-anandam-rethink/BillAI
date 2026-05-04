using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimAttachmentService
    {
        Task<AttachmentsResponseModel> GetForClaimAsync(IdWithUserInfo model);
        Task<ClaimAttachmentItem> Get(int id);
        Task<List<ClaimAttachmentItem>> Save(List<ClaimAttachmentItem> items, int claimId, int memberId, int accountInfoId);
        Task<ClaimAttachmentItem> Delete(ClaimAttachmentItem item, int memberId, int accountInfoId);
        //Task<byte[]> DownloadAttachmentFile(int claimId, int encounterAttachmentId);
        Task<int> UploadFileAsync(ClaimUploadModelWithUserInfo model);
        Task RenameAttachmentAsync(RenameAttachmentModelWithUserInfo model);
        Task DeleteUpload(IdWithUserInfo model);
        Task<string> GetUploadAsync(IdWithUserInfo model);
    }
}

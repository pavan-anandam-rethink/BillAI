using BillingService.Domain.Models.PaymentPosting;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Payment
{
    public interface IPaymentAttachmentService
    {
        Task<int> UploadFile(PaymentUploadModelWithUserInfo model);
        Task<PaymentAttachmentReturnModel> GetUpload(int id);
        Task<AttachmentsResponseModel> GetPaymentAttachmentsAsync(GetByIdSortFilterWithUserInfo model);
        Task DeleteUpload(IdWithUserInfo model);
        Task DeleteUploads(DeleteAttachmentsModelWithUserInfo model);
        Task RenameAttachmentAsync(RenameAttachmentModelWithUserInfo model);
    }
}
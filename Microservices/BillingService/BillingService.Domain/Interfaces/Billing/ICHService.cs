using BillingService.Domain.Models.Claims;
using Rethink.Services.Common.Models.Claim;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface ICHService : IBlobBackupService
    {
        Task<bool> UploadFileAsync(ClaimUploadModelWithUserInfo model);
        Task<bool> UploadEDIResponseFile(DownloadSftpDataModel fileStreams);
        Task<bool> UploadERAErrorFileAsync(ERAUploadModel model);
        
    }
    public interface IBlobBackupService
    {
        Task<bool> UploadEDIResponseFilesToBlobBackup(UploadAvailityFilesModel fileStreams);
    }
}

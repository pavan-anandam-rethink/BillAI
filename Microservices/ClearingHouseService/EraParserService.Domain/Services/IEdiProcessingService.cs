using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Rethink.Services.Common.Handlers;
using System.IO;
using System.Threading.Tasks;
using Thon.Hotels.FishBus;

namespace EraParserService.Domain.Services
{
    public interface IEdiProcessingService
    {
        Task<HandlerResult> ProcessFile(EdiDownloadData ediDownloadData, Stream ediStream);
        Task<HandlerResult> ProcessFile(EdiDownloadData ediDownloadData);
        Task uploadToBilling(BillingFolderStructureModel model, BlobFolderNames folderName, BlobFolderNames? subFolderName);
    }
}
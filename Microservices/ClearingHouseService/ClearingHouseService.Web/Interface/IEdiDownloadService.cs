using ClearingHouseService.Web.Service;

namespace ClearingHouseService.Web.Interface
{
    public interface IEdiDownloadService
    {
        Task<List<(MemoryStream, string)>> downloadEdiDataFromSftp(ClearingHouseDetailsModel ediDownloadStartData);
        Task<bool> DeleteFileFromSftp(ClearingHouseDetailsModel ediDownloadStartData, string fileName);
    }
}

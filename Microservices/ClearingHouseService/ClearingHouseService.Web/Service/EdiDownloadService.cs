using ClearingHouseService.Web.Interface;

namespace ClearingHouseService.Web.Service
{
    public class EdiDownloadService : IEdiDownloadService
    {        
        private readonly ILogger<EdiDownloadService> _logger;
        private readonly IClearingHouseUploaderFactory _clearingHouseUploaderFactory;
        public EdiDownloadService(ILogger<EdiDownloadService> logger, IClearingHouseUploaderFactory clearingHouseUploaderFactory)
        {          
            _logger = logger;
            _clearingHouseUploaderFactory = clearingHouseUploaderFactory;
        }

        public async Task<List<(MemoryStream, string)>> downloadEdiDataFromSftp(ClearingHouseDetailsModel ediDownloadStartData)
        {
            _logger.LogInformation("Starting download of EDI data from SFTP for Clearing House: {ClearingHouse}", ediDownloadStartData.Title);
            var uploader = _clearingHouseUploaderFactory.Create();
            var fileStreams= await uploader.DownloadFilesFromSftpAsync(ediDownloadStartData);
            return fileStreams;
          

        }

        public async Task<bool> DeleteFileFromSftp(ClearingHouseDetailsModel ediDownloadStartData, string fileName)
        {
            _logger.LogInformation("Starting deletion of file {FileName} from SFTP for Clearing House: {ClearingHouse}", fileName, ediDownloadStartData.Title);
           
            var uploader = _clearingHouseUploaderFactory.Create();
            return await uploader.DeleteFileFromSftpAsync(ediDownloadStartData, fileName);
        }
    }
}

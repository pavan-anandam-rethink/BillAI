using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.Claim
{
    public class DownloadSftpDataModel
    {
        public int clearingHouseId { get; set; }
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
    }
    public class UploadAvailityFilesModel
    {       
        public List<(MemoryStream, string)> files { get; set; }
        public string FilePath { get; set; }
    }
}

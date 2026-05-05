namespace ClearingHouseService.Web.Service
{
    public class ClearingHouseDetailsModel
    {
        public string Title { get; set; }
        public string UrlLink { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public int Port { get; set; }
        public string TaxId { get; set; }
        public int ClearingHouseId { get; set; }       
        public string UploadDirectory { get; set; }
        public string DownloadDirectory { get; set; }

        public int ClaimId { get; set; }
    }
}

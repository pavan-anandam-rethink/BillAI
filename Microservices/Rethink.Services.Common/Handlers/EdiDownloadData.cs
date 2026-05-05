using System;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Handlers
{
    [ExcludeFromCodeCoverage]
    public class EdiDownloadData
    {
        /// <summary>
        ///  The time/date the file was downloaded
        /// </summary>
        public DateTime DownloadDateTime { get; set; }

        /// <summary>
        /// The azure url needed to retrieve the file
        /// </summary>
        public string ContainerName { get; set; }
        /// <summary>
        /// This contains the filename captured during the download process
        /// </summary>
        public string FileIdentifier { get; set; }

        /// <summary>
        /// EraData is the contents of the EdiFile. It is only available if it is smaller than
        /// 250k. Otherwise the recipient will have to connect to azure storage and download it.
        /// </summary>
        public string EdiData { get; set; } = null;

        public int AccountInfoId { get; set; }
        public int? PaymentEraUploadId { get; set; } = null;
        public int ClearingHouseId { get; set; }
    }
}

using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Handlers
{
    [ExcludeFromCodeCoverage]
    public class EdiUploadStart : EdiDownloadStart
    {
        public int ClaimId { get; set; }
        public int ClaimSubmissionId { get; set; }
        public int MemberId { get; set; }
        public int AccountInfoId { get; set; }
        public string EdiData { get; set; }
    }
}

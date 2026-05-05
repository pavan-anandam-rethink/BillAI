using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Handlers
{
    [ExcludeFromCodeCoverage]
    public class EdiUploadEnd
    {
        public int ClaimId { get; set; }
        public int ClaimSubmissionId { get; set; }
        public int MemberId { get; set; }
        public int AccountInfoId { get; set; }
        public bool IsSuccess { get; set; }
    }
}

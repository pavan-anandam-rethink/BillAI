namespace Rethink.Services.Common.Handlers
{
    public class ClaimCreateEnd
    {
        public int AccountInfoId { get; set; }
        public int ClaimId { get; set; }
        public int? ClientId { get; set; }
        public int? RenderingProviderTypeId { get; set; }
        public int? RenderingProviderId { get; set; }
        public int? FunderId { get; set; }
        public int? ChildProfileAuthorizationId { get; set; }
    }
}

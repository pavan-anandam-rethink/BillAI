namespace Rethink.Services.Domain.Services.RethinkServices
{
    public sealed class RethinkBillingRequestContext : IRethinkBillingRequestContext
    {
        public string? SessionKey { get; set; }

        public int? AccountInfoId { get; set; }
    }
}

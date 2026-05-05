namespace Rethink.Services.Domain.Services.RethinkServices
{
    /// <summary>
    /// Per-request billing context (JWT claims) used for session-scoped master data cache keys.
    /// </summary>
    public interface IRethinkBillingRequestContext
    {
        string? SessionKey { get; set; }

        int? AccountInfoId { get; set; }
    }
}

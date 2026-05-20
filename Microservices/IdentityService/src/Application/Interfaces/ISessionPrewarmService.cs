namespace IdentityService.Application.Interfaces;

public interface ISessionPrewarmService
{
    Task WarmAsync(int accountId, string billingSessionKey, CancellationToken cancellationToken = default);
}

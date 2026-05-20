namespace IdentityService.Application.Interfaces;

public interface ITokenValidationApiService
{
    Task<string?> ValidateRethinkTokenAsync(string rethinkToken, CancellationToken cancellationToken = default);
}

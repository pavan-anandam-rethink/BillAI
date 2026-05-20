using IdentityService.Application.DTOs;

namespace IdentityService.Application.Interfaces;

public interface IAccountService
{
    Task<AccountInfoDto?> GetAccountInfoAsync(int accountId, CancellationToken cancellationToken = default);
}

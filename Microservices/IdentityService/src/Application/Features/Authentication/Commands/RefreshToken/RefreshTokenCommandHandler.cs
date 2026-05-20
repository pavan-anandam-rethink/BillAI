using IdentityService.Application.DTOs;
using IdentityService.Application.Exceptions;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Events;
using IdentityService.Domain.ValueObjects;
using IdentityService.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Features.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    ITokenService tokenService,
    IConfiguration config,
    IMediator mediator,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, AuthenticatedResponseDto>
{
    public async Task<AuthenticatedResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing token refresh request");

        var jwtSettings = new JwtSettings
        {
            Key = config["Jwt:Key"]!,
            Issuer = config["Jwt:Issuer"]!,
            Audience = config["Jwt:Audience"] ?? config["Jwt:Issuer"]!
        };

        var principal = tokenService.GetPrincipalFromExpiredToken(jwtSettings, request.AccessToken);
        var claims = principal.Claims;

        var authRequest = new AuthenticateRequestDto
        {
            AccountInfoId = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.AccountInfoId)?.Value ?? string.Empty,
            MemberId = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.MemberId)?.Value ?? string.Empty,
            MemberName = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.MemberName)?.Value ?? string.Empty,
            MemberRole = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.MemberRole)?.Value ?? string.Empty,
            ImpersonationUserObjectId = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.ImpersonatedUser)?.Value ?? string.Empty,
            ImpersonationUserName = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.ImpersonationUserName)?.Value ?? string.Empty,
            ImpersonationUserEmail = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.ImpersonationUserEmail)?.Value ?? string.Empty,
            BillingSessionKey = claims.SingleOrDefault(c => c.Type == AuthConstants.ClaimTypes.BillingSessionKey)?.Value ?? string.Empty,
            Permissions = []
        };

        foreach (var permission in claims.Where(c => c.Type == AuthConstants.ClaimTypes.Permissions))
        {
            authRequest.Permissions[permission.Value.ToLower()] = true;
        }

        var newJwtToken = await tokenService.GenerateAccessTokenAsync(jwtSettings, authRequest);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        // Publish domain event
        await mediator.Publish(new TokenRefreshedEvent(
            authRequest.MemberId,
            authRequest.AccountInfoId,
            DateTime.UtcNow), cancellationToken);

        return new AuthenticatedResponseDto
        {
            Token = newJwtToken,
            RefreshToken = newRefreshToken,
            BillingSessionKey = authRequest.BillingSessionKey
        };
    }
}

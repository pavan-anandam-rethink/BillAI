using IdentityService.Application.DTOs;
using MediatR;

namespace IdentityService.Application.Features.Authentication.Commands.SsoLogin;

public record SsoLoginCommand(string RethinkToken) : IRequest<AuthenticatedResponseDto>;

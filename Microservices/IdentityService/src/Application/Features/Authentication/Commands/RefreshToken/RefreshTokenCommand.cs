using IdentityService.Application.DTOs;
using MediatR;

namespace IdentityService.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken) : IRequest<AuthenticatedResponseDto>;

using Asp.Versioning;
using IdentityService.Application.DTOs;
using IdentityService.Application.Features.Authentication.Commands.RefreshToken;
using IdentityService.Application.Features.Authentication.Commands.SsoLogin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[AllowAnonymous]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// SSO Login - validates a Rethink token and returns JWT credentials
    /// </summary>
    [HttpPost("sso-login")]
    [ProducesResponseType(typeof(AuthenticatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SsoLogin([FromBody] TokenRequestDto request)
    {
        var command = new SsoLoginCommand(request.Token);
        var result = await mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Refresh an expired JWT token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticatedResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] TokenRequestDto request)
    {
        var command = new RefreshTokenCommand(request.Token);
        var result = await mediator.Send(command);
        return Ok(result);
    }
}

using IdentityService.Application.DTOs;
using IdentityService.Application.Features.Authentication.Commands.RefreshToken;
using IdentityService.Application.Features.Authentication.Commands.SsoLogin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

/// <summary>
/// Legacy SSO controller maintaining backward compatibility with existing clients.
/// New clients should use AuthController (api/v1/auth/).
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[AllowAnonymous]
[ApiExplorerSettings(GroupName = "legacy")]
public class SSOController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SSOLogin([FromBody] TokenRequestDto request)
    {
        var command = new SsoLoginCommand(request.Token);
        var result = await mediator.Send(command);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Refresh([FromBody] TokenRequestDto request)
    {
        var command = new RefreshTokenCommand(request.Token);
        var result = await mediator.Send(command);
        return Ok(result);
    }
}

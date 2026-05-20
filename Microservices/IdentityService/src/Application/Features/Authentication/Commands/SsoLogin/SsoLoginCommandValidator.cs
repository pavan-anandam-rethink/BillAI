using FluentValidation;

namespace IdentityService.Application.Features.Authentication.Commands.SsoLogin;

public class SsoLoginCommandValidator : AbstractValidator<SsoLoginCommand>
{
    public SsoLoginCommandValidator()
    {
        RuleFor(x => x.RethinkToken)
            .NotEmpty().WithMessage("Rethink token is required.");
    }
}

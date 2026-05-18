using FluentValidation;

namespace BillingService.App.Application.Claims.Queries;

public sealed class GetClaimHeadersQueryValidator : AbstractValidator<GetClaimHeadersQuery>
{
    public GetClaimHeadersQueryValidator()
    {
        RuleFor(x => x.JsonPayload)
            .NotEmpty()
            .MaximumLength(2_000_000);
    }
}


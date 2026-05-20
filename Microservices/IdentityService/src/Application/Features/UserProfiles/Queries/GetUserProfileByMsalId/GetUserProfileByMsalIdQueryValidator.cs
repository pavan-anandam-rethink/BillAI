using FluentValidation;

namespace IdentityService.Application.Features.UserProfiles.Queries.GetUserProfileByMsalId;

public class GetUserProfileByMsalIdQueryValidator : AbstractValidator<GetUserProfileByMsalIdQuery>
{
    public GetUserProfileByMsalIdQueryValidator()
    {
        RuleFor(x => x.MsalObjectId)
            .NotEmpty().WithMessage("MSAL Object ID is required.");
    }
}

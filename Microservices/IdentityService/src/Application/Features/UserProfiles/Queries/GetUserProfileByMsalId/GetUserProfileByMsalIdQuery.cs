using IdentityService.Application.DTOs;
using MediatR;

namespace IdentityService.Application.Features.UserProfiles.Queries.GetUserProfileByMsalId;

public record GetUserProfileByMsalIdQuery(string MsalObjectId, bool UseCache = true) : IRequest<UserProfileDto?>;

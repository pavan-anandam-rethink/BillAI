using AutoMapper;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserProfile, UserProfileDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));
    }
}

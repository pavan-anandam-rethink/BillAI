using AutoMapper;
using LoginService.Domain.Models;
using LoginService.Infrastructure.Entities;

namespace LoginService.Infrastructure.Mapping
{
    public class UserProfileMapping : Profile
    {
        public UserProfileMapping()
        {
            CreateMap<UserProfileEntity, UserProfile>()
                .ForMember(dest => dest.Id, src => src.MapFrom(x => x.Id))
                .ForMember(dest => dest.FirstName, src => src.MapFrom(x => x.FirstName))
                .ForMember(dest => dest.LastName, src => src.MapFrom(x => x.LastName))
                .ForMember(dest => dest.Email, src => src.MapFrom(x => x.Email))
                .ForMember(dest => dest.MsalObjectId, src => src.MapFrom(x => x.MsalObjectId))
                .ForMember(dest => dest.CreatedOn, src => src.MapFrom(x => x.Metadata.DateCreated))
                ;
        }
    }
}

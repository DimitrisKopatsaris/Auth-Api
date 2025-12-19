using AutoMapper;
using AuthApi.Models;
using AuthApi.DTOs;

namespace AuthApi.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity -> DTO
            CreateMap<User, UserDto>();

            // DTO -> Entity (for register)
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        }
    }
}

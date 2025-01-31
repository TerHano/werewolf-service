using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Mappers;

public class PlayerRoleMapper : Profile
{
    public PlayerRoleMapper()
    {
        CreateMap<PlayerRoleEntity, PlayerRoleDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Nickname, opt => opt.MapFrom(src => src.PlayerRoom.NickName))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

    }
}
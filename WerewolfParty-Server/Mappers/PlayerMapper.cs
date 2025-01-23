using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Mappers;

public class PlayerMapper : Profile
{
    public PlayerMapper()
    {
        CreateMap<PlayerRoomEntity, PlayerDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PlayerId))
            .ForMember(dest => dest.Nickname, opt => opt.MapFrom(src => src.NickName))
            .ForMember((dest) => dest.AvatarIndex, opt => opt.MapFrom(src => src.AvatarIndex));
    }
}
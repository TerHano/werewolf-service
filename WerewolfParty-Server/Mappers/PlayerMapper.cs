using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Mappers;

public class PlayerMapper: Profile
{
    public PlayerMapper()
    {
        CreateMap<PlayerRoomEntity, PlayerDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember((dest) => dest.AvatarIndex, opt => opt.MapFrom(src => src.AvatarIndex));

    }
}
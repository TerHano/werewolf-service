using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Mappers;

public class PlayerGameActionMapper : Profile
{
    public PlayerGameActionMapper()
    {
        CreateMap<RoomGameActionEntity, PlayerGameActionDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Player, opt => opt.MapFrom(src => src.PlayerRole))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
            .ForMember((dest) => dest.AffectedPlayer, opt => opt.MapFrom(src => src.AffectedPlayerRole));
    }
}
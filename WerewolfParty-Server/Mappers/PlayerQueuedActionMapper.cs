using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Mappers;

public class PlayerQueuedActionMapper : Profile
{
    public PlayerQueuedActionMapper()
    {
        CreateMap<RoomGameActionEntity, PlayerQueuedActionDTO>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PlayerRoleId, opt => opt.MapFrom(src => src.PlayerRoleId))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
            .ForMember((dest) => dest.AffectedPlayerRoleId, opt => opt.MapFrom(src => src.AffectedPlayerRoleId));
    }
}
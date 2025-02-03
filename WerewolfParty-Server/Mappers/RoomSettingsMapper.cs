using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Mappers;

public class RoomSettingsMapper : Profile
{
    public RoomSettingsMapper()
    {
        CreateMap<RoomSettingsEntity, RoomSettingsDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.NumberOfWerewolves, opt => opt.MapFrom(src => src.NumberOfWerewolves))
            .ForMember(dest => dest.SelectedRoles, opt => opt.MapFrom(src => src.SelectedRoles))
            .ForMember(dest => dest.ShowGameSummary, opt => opt.MapFrom(src => src.ShowGameSummary))
            .ForMember(dest => dest.AllowMultipleSelfHeals, opt => opt.MapFrom(src => src.AllowMultipleSelfHeals));
    }
}
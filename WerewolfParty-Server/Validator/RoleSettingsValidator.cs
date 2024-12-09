using FluentValidation;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Validator;

public class RoleSettingsValidator: AbstractValidator<RoleSettingsEntity>
{
    public RoleSettingsValidator()
    {
        RuleFor(x => x.Werewolves).IsInEnum().WithMessage("Invalid Werewolves Amount");
        RuleFor(x=>x.SelectedRoles).IsInEnum().WithMessage("Invalid Selected Roles");
    }
}
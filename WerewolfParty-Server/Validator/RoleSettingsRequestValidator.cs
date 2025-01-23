using FluentValidation;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.Validator;

public class RoleSettingsRequestValidator : AbstractValidator<UpdateRoleSettingsRequest>
{
    public RoleSettingsRequestValidator()
    {
        RuleFor(x => x.NumberOfWerewolves).GreaterThanOrEqualTo(1).WithMessage("Invalid Werewolves Amount");
        RuleForEach(x => x.SelectedRoles).IsInEnum().WithMessage("Invalid Selected Roles");
    }
}
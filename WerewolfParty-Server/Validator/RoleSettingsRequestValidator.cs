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
        // Short enough to keep nights moving, long enough that a step is not over before a
        // phone has woken up.
        RuleFor(x => x.NightStepSeconds).InclusiveBetween(10, 300)
            .WithMessage("Night step length must be between 10 and 300 seconds");
    }
}
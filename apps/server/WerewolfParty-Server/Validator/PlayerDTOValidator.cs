using FluentValidation;
using WerewolfParty_Server.DTO;

namespace WerewolfParty_Server.Validator;

public class PlayerDTOValidator : AbstractValidator<PlayerDTO>
{
    public PlayerDTOValidator()
    {
        RuleFor(x => x.Nickname).NotEmpty().WithMessage("Nickname is required");
    }
}
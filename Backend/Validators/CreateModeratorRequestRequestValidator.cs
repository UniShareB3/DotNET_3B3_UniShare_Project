using Backend.Features.ModeratorRequest.CreateModeratorRequest;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace Backend.Validators;

public class CreateModeratorRequestRequestValidator :  AbstractValidator<CreateModeratorRequestRequest>
{
    public CreateModeratorRequestRequestValidator(CreateModeratorRequestDtoValidator moderatorRequestDtoValidator)
    {
        RuleFor(r => r.Dto)
            .SetValidator(moderatorRequestDtoValidator);
    }
}
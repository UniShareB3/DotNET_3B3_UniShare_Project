using Backend.Features.ModeratorAssignment.CreateModeratorAssignment;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace Backend.Validators;

public class CreateModeratorAssignmentRequestValidator :  AbstractValidator<CreateModeratorAssignmentRequest>
{
    public CreateModeratorAssignmentRequestValidator(CreateModeratorAssignmentDtoValidator moderatorAssignmentDtoValidator)
    {
        RuleFor(r => r.Dto)
            .SetValidator(moderatorAssignmentDtoValidator);
    }
}
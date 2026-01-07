using Backend.Features.ModeratorAssignment.CreateModeratorAssignment;
using FluentValidation;

namespace Backend.Validators;

public class CreateModeratorAssignmentRequestValidator :  AbstractValidator<CreateModeratorAssignmentRequest>
{
    public CreateModeratorAssignmentRequestValidator(CreateModeratorAssignmentDtoValidator moderatorAssignmentDtoValidator)
    {
        RuleFor(r => r.Dto)
            .SetValidator(moderatorAssignmentDtoValidator);
    }
}
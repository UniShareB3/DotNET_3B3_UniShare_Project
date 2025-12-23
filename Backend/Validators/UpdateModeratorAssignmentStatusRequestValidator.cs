using Backend.Features.ModeratorAssignment.UpdateModeratorAssignment;
using FluentValidation;

namespace Backend.Validators;

public class UpdateModeratorAssignmentStatusRequestValidator : AbstractValidator<UpdateModeratorAssignmentStatusRequest>
{
    public UpdateModeratorAssignmentStatusRequestValidator(UpdateModeratorAssignmentStatusDtoValidator dtoValidator)
    {
        RuleFor(r => r.Dto)
            .SetValidator(dtoValidator);
    }
}


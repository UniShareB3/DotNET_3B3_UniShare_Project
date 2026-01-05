using AutoMapper;
using Backend.Data;
using Backend.Features.ModeratorAssignment.DTO;
using Backend.Features.ModeratorAssignment.Enums;
using Backend.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class CreateModeratorAssignmentDtoValidator : AbstractValidator<CreateModeratorAssignmentDto>
{
    private readonly ApplicationContext _appContext;
    private readonly UserManager<User> _userManager;

    public CreateModeratorAssignmentDtoValidator(ApplicationContext appContext, UserManager<User> userManager)
    {
        _appContext = appContext;
        _userManager = userManager;
        
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");
        
        RuleFor(x => x)
            .MustAsync(async (assignment, _) => await IsAssignmentTimeValid(assignment))
            .WithMessage("A month must pass between submitting a moderator assignment again");
        
        RuleFor(x => x)
            .MustAsync(async (assignment, _) => await IsModeratorAlready(assignment))
            .WithMessage("You already have a moderator assignment");
        
        RuleFor(x => x)
            .MustAsync(async (assignment, _) => await IsRequestAlreadyPending(assignment))
            .WithMessage("You already have a pending moderator assignment request");    
    }
    private async Task<bool> IsAssignmentTimeValid(CreateModeratorAssignmentDto dto)
    {
        var lastRejectedModeratorAssignment =
            _appContext.ModeratorAssignments
                .Where(r => r.UserId == dto.UserId && r.Status == ModeratorAssignmentStatus.REJECTED)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefault();
        
        if (lastRejectedModeratorAssignment == null)
            return true;
        if(DateTime.UtcNow.AddDays(-30) >  lastRejectedModeratorAssignment.CreatedDate)
            return true;
        return false;
    }

    private async Task<bool> IsModeratorAlready(CreateModeratorAssignmentDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        IList<string> roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            if (role == "Moderator")
                return false;
        }
        return true;
    }
    
    private async Task<bool> IsRequestAlreadyPending(CreateModeratorAssignmentDto dto)
    {
        var existingAssignment = await _appContext.ModeratorAssignments
            .FirstOrDefaultAsync(mr => mr.UserId == dto.UserId 
                && mr.Status == ModeratorAssignmentStatus.PENDING);
        return existingAssignment == null;
    }
}

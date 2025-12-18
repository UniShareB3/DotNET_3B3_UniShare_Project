using AutoMapper;
using Backend.Data;
using Backend.Features.ModeratorRequest.DTO;
using Backend.Features.ModeratorRequest.Enums;
using Backend.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Backend.Validators;

public class CreateModeratorRequestDtoValidator : AbstractValidator<CreateModeratorRequestDto>
{
    private readonly ApplicationContext _appContext;
    private readonly UserManager<User> _userManager;

    public CreateModeratorRequestDtoValidator(ApplicationContext appContext, UserManager<User> userManager)
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
            .MustAsync(async (request, _) => await IsRequestTimeValid(request))
            .WithMessage("A month must pass between submitting a moderator request again");
        
        RuleFor(x => x)
            .MustAsync(async (request, _) => await IsModeratorAlready(request))
            .WithMessage("You already have a moderator request");
    }
    private async Task<bool> IsRequestTimeValid(CreateModeratorRequestDto dto)
    {
        var lastRejectedModeratoRequest =
            _appContext.ModeratorRequests
                .Where(r => r.UserId == dto.UserId && r.Status == ModeratorRequestStatus.REJECTED)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefault();
        
        if (lastRejectedModeratoRequest == null)
            return true;
        if(DateTime.UtcNow.AddDays(-30) >  lastRejectedModeratoRequest.CreatedDate)
            return true;
        return false;
    }

    private async Task<bool> IsModeratorAlready(CreateModeratorRequestDto dto)
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
}


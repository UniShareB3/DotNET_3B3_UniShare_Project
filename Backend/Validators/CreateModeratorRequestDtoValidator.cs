using Backend.Constants;
using Backend.Data;
using Backend.Features.ModeratorRequest.DTO;
using Backend.Features.ModeratorRequest.Enums;
using Backend.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class CreateModeratorRequestDtoValidator : AbstractValidator<CreateModeratorRequestDto>
{
    private readonly ApplicationContext _context;
    private readonly UserManager<User> _userManager;

    public CreateModeratorRequestDtoValidator(ApplicationContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .MustAsync(UserExists)
            .WithMessage("User does not exist")
            .MustAsync(UserIsNotAlreadyModerator)
            .WithMessage("User is already a moderator")
            .MustAsync(NoDeclinedRequestInLastMonth)
            .WithMessage("Cannot submit a new request. A previous request was declined within the last month.")
            .MustAsync(NoPendingRequest)
            .WithMessage("Cannot submit a new request. There is already a pending request for this user.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(ValidationConstants.MaxReasonLength)
            .WithMessage($"Reason cannot exceed {ValidationConstants.MaxReasonLength} characters")
            .MinimumLength(20)
            .WithMessage("Reason must be at least 20 characters long");
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        return user != null;
    }

    private async Task<bool> UserIsNotAlreadyModerator(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return true; 

        var roles = await _userManager.GetRolesAsync(user);
        return !roles.Contains("Moderator");
    }

    private async Task<bool> NoDeclinedRequestInLastMonth(Guid userId, CancellationToken cancellationToken)
    {
        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

        var declinedRequest = await _context.ModeratorRequests
            .Where(mr => mr.UserId == userId 
                         && mr.Status == ModeratorRequestStatus.REJECTED 
                         && mr.ReviewedDate != null
                         && mr.ReviewedDate >= oneMonthAgo)
            .OrderByDescending(mr => mr.ReviewedDate)
            .FirstOrDefaultAsync(cancellationToken);

        return declinedRequest == null;
    }

    private async Task<bool> NoPendingRequest(Guid userId, CancellationToken cancellationToken)
    {
        var pendingRequest = await _context.ModeratorRequests
            .AnyAsync(mr => mr.UserId == userId 
                            && mr.Status == ModeratorRequestStatus.PENDING, 
                      cancellationToken);

        return !pendingRequest;
    }
}

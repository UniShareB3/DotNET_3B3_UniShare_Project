using Backend.Data;
using Backend.Features.Shared.Auth;
using Backend.Features.Users;
using Backend.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    private readonly UserManager<User> _userManager;

    public ChangePasswordRequestValidator(UserManager<User> userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.ChangePasswordDto).NotNull();

        RuleFor(x => x.ChangePasswordDto.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.ChangePasswordDto.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .CustomAsync(ValidatePasswordStrengthAsync)
            .CustomAsync(ValidatePasswordNotInDatabaseAsync);
    }

    private async Task ValidatePasswordStrengthAsync(
        string password,
        ValidationContext<ChangePasswordRequest> context,
        CancellationToken cancellationToken)
    {
        var request = context.InstanceToValidate;
        var user = await _userManager.FindByIdAsync(request.ChangePasswordDto.UserId.ToString());
        if (user == null)
        {
            context.AddFailure(nameof(request.ChangePasswordDto.UserId), "User not found.");
            return;
        }

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    context.AddFailure(nameof(request.ChangePasswordDto.NewPassword), error.Description);
                }
            }
        }
    }

    private async Task ValidatePasswordNotInDatabaseAsync(
        string password,
        ValidationContext<ChangePasswordRequest> context,
        CancellationToken cancellationToken)
    {
        // Check if any user in the database has this password hash
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        
        foreach (var user in users)
        {
            var result = await _userManager.CheckPasswordAsync(user, password);
            if (result)
            {
                context.AddFailure(nameof(context.InstanceToValidate.ChangePasswordDto.NewPassword), 
                    "Please choose a different password.");
                return;
            }
        }
    }
}


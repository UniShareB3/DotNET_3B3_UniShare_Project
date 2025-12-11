using Backend.Data;
using Backend.Features.Users;
using Backend.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    private readonly ApplicationContext _dbContext;
    private readonly UserManager<User> _userManager;

    public UpdateUserRequestValidator(ApplicationContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;

        RuleFor(x => x.UpdateUserDto).NotNull();

        RuleFor(x => x.UpdateUserDto.FirstName)
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.UpdateUserDto.FirstName));

        RuleFor(x => x.UpdateUserDto.LastName)
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.UpdateUserDto.LastName));

        RuleFor(x => x)
            .CustomAsync(ValidateEmailUniquenessAsync)
            .When(x => !string.IsNullOrWhiteSpace(x.UpdateUserDto.Email));

        RuleFor(x => x)
            .CustomAsync(ValidatePasswordStrengthAsync)
            .CustomAsync(ValidatePasswordNotInDatabaseAsync)
            .When(x => !string.IsNullOrWhiteSpace(x.UpdateUserDto.Password));

        RuleFor(x => x)
            .CustomAsync(ValidateUniversityExistsAsync)
            .When(x => !string.IsNullOrWhiteSpace(x.UpdateUserDto.UniversityName));
    }

    private async Task ValidateEmailUniquenessAsync(
        UpdateUserRequest request, 
        ValidationContext<UpdateUserRequest> context, 
        CancellationToken cancellationToken)
    {
        var email = request.UpdateUserDto.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        var existingUser = await _userManager.FindByEmailAsync(email);
        
        if (existingUser != null && existingUser.Id != request.UserId)
        {
            context.AddFailure(nameof(request.UpdateUserDto.Email), "Email is already in use by another user.");
        }
        
    }

    private async Task ValidatePasswordStrengthAsync(
        UpdateUserRequest request, 
        ValidationContext<UpdateUserRequest> context, 
        CancellationToken cancellationToken)
    {
        var password = request.UpdateUserDto.Password;
        if (string.IsNullOrWhiteSpace(password))
            return;

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return;

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    context.AddFailure(nameof(request.UpdateUserDto.Password), error.Description);
                }
            }
        }
    }

    private async Task ValidatePasswordNotInDatabaseAsync(
        UpdateUserRequest request, 
        ValidationContext<UpdateUserRequest> context, 
        CancellationToken cancellationToken)
    {
        var password = request.UpdateUserDto.Password;
        if (string.IsNullOrWhiteSpace(password))
            return;

        // Check if any user in the database has this password hash
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        
        foreach (var user in users)
        {
            var result = await _userManager.CheckPasswordAsync(user, password);
            if (result)
            {
                context.AddFailure(nameof(request.UpdateUserDto.Password), 
                    "This password is already in use. Please choose a different password.");
                return;
            }
        }
    }

    private async Task ValidateUniversityExistsAsync(
        UpdateUserRequest request, 
        ValidationContext<UpdateUserRequest> context, 
        CancellationToken cancellationToken)
    {
        var universityName = request.UpdateUserDto.UniversityName;
        if (string.IsNullOrWhiteSpace(universityName))
            return;

        var exists = await _dbContext.Universities
            .AnyAsync(u => u.Name == universityName, cancellationToken);
        
        if (!exists)
        {
            context.AddFailure(nameof(request.UpdateUserDto.UniversityName), 
                $"University '{universityName}' does not exist.");
        }
    }
}

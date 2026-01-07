using Backend.Features.Users.RegisterUser;
using Backend.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class RegisterUserValidator:AbstractValidator<RegisterUserRequest>
{
    private readonly ApplicationContext _context;
    private readonly ILogger<RegisterUserValidator> _logger;
    
    public RegisterUserValidator(ApplicationContext context, ILogger<RegisterUserValidator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        RuleFor(x => x.RegisterUserDto.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MustAsync(async (email, _) => !await _context.Users.AnyAsync(u => u.Email == email))
            .WithMessage("Email already in use.");

        RuleFor(x => x.RegisterUserDto.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.RegisterUserDto.FirstName)
            .NotEmpty().WithMessage("FirstName is required.")
            .MaximumLength(100).WithMessage("FirstName cannot exceed 100 characters.");
        
        RuleFor(x => x.RegisterUserDto.LastName)
            .NotEmpty().WithMessage("LastName is required.")
            .MaximumLength(100).WithMessage("LastName cannot exceed 100 characters.");
        
        RuleFor(x => x.RegisterUserDto.UniversityName)
            .NotEmpty().WithMessage("UniversityName is required.")
            .MaximumLength(200).WithMessage("UniversityName cannot exceed 200 characters.")
            .MustAsync(async (universityName, _) => await UniversityExists(universityName))
            .WithMessage("University does not exist.");
    }
    
    private async Task<bool> UniversityExists(string universityName)
    {
       var exists = await _context.Universities
            .AnyAsync(u => u.Name == universityName);
       
       if (!exists)
       {
           _logger.LogWarning("University {UniversityName} does not exist in the database.", universityName);
       }
       
       return exists;
    }
}
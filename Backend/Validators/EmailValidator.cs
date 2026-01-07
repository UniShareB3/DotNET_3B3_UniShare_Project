using Backend.Persistence;
using Backend.Data;
using Microsoft.AspNetCore.Identity;

namespace Backend.Validators;


public class EmailValidator(ApplicationContext dbContext) : IUserValidator<User>
{
    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
    {
        if (user.UniversityId == null)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var university = dbContext.Set<University>()
            .AsEnumerable().FirstOrDefault(u => u.Id == user.UniversityId);
        
        Console.WriteLine($"Validating email: {user.Email} for university domain: {university?.EmailDomain}");
        
        // If university not found or doesn't have an email domain, skip validation
        if (university == null || string.IsNullOrEmpty(university.EmailDomain))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "UniversityNotFound",
                Description = "University not found or has no email domain."
            }));
        }

        if (user.Email == null)
            return Task.FromResult(IdentityResult.Failed(new IdentityError()
            {
                Code = "InvalidEmail",
                Description = "Email cannot be null."
            }));
        var domainParts = university.EmailDomain.Split('@');
        var domain = domainParts[^1];
        var isValid = System.Text.RegularExpressions.Regex.IsMatch(user.Email,
            $@"^[^@\s]+@(?:student\.)?{domain}$",
            System.Text.RegularExpressions.RegexOptions.None);
            
        if (isValid) 
            return Task.FromResult(IdentityResult.Success);
            
        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "InvalidEmailDomain",
            Description = $"Email must belong to the {domain} domain."
        }));
    }
}
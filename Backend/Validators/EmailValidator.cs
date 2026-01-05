﻿using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;
using Backend.Data;
using Microsoft.AspNetCore.Identity;

public class EmailValidator(ApplicationContext dbContext) : IUserValidator<User>
{
    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
    {
        // If user has no university ID, skip email domain validation
        // This can happen during password reset or for users without university
        if (user.UniversityId == null)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        University? university = dbContext.Set<University>().ToList()
            .Where(u => u.Id == user.UniversityId).FirstOrDefault();
        
        Console.WriteLine($"Validating email: {user.Email} for university domain: {university?.EmailDomain}");
        
        // If university not found or has no email domain, skip validation
        if (university == null || string.IsNullOrEmpty(university.EmailDomain))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "UniversityNotFound",
                Description = "University not found or has no email domain."
            }));
        }

        if (user.Email != null) {
            string domain = university.EmailDomain.Split('@').Last();
            bool isValid = System.Text.RegularExpressions.Regex.IsMatch(user.Email,
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
        return Task.FromResult(IdentityResult.Failed(new IdentityError()
        {
            Code = "InvalidEmail",
            Description = "Email cannot be null."
        }));
    }
}
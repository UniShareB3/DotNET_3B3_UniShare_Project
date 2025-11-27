using Backend.Data;
using Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Validators;

public class UniversityValidator(ApplicationContext dbContext) : IUserValidator<User>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
    {
        if (user.UniversityId == Guid.Empty)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidUniversity",
                Description = "The university ID is not set."
            });
        }

        var universityExists = await dbContext.Universities.AnyAsync(u => u.Id == user.UniversityId);
        if (!universityExists)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UniversityNotFound",
                Description = "The specified university does not exist."
            });
        }

        
        return IdentityResult.Success;
    }
}
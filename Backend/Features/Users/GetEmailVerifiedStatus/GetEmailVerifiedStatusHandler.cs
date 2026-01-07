using MediatR;
using Microsoft.AspNetCore.Identity;
using Backend.Data;

namespace Backend.Features.Users.GetEmailVerifiedStatus;

public class GetEmailVerifiedStatusHandler(UserManager<User> userManager)
    : IRequestHandler<GetEmailVerifiedStatusRequest, IResult>
{
    public async Task<IResult> Handle(GetEmailVerifiedStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        return user == null ? Results.NotFound(new { message = "User not found" }) : Results.Ok(new { emailVerified = user.NewEmailConfirmed });
    }
}
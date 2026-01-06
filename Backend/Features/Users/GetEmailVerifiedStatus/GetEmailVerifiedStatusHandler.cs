using MediatR;
using Microsoft.AspNetCore.Identity;
using Backend.Data;

namespace Backend.Features.Users;

public class GetEmailVerifiedStatusHandler 
    : IRequestHandler<GetEmailVerifiedStatusRequest, IResult>
{
    private readonly UserManager<User> _userManager;

    public GetEmailVerifiedStatusHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IResult> Handle(GetEmailVerifiedStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        return Results.Ok(new { emailVerified = user.NewEmailConfirmed });
    }
}
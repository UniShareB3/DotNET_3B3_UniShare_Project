using Backend.Features.Shared.IAM.DTO;
using MediatR;

namespace Backend.Features.Shared.IAM.ChangePassword;

public record ChangePasswordRequest(ChangePasswordDto ChangePasswordDto) : IRequest<IResult>;


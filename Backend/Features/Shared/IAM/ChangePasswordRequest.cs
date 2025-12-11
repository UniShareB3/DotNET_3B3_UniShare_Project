using Backend.Features.Shared.IAM.DTO;
using Backend.Features.Users.Dtos;
using MediatR;

namespace Backend.Features.Shared.Auth;

public record ChangePasswordRequest(ChangePasswordDto ChangePasswordDto) : IRequest<IResult>;


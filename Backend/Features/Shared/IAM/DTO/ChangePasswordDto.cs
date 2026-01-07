namespace Backend.Features.Shared.IAM.DTO;

public abstract record ChangePasswordDto(
    string NewPassword,
    Guid UserId
);


namespace Backend.Features.Shared.IAM.DTO;

public record ChangePasswordDto(
    string NewPassword,
    Guid UserId
);


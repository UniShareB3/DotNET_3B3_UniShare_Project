namespace Backend.Features.Shared.IAM.DTO;

public record VerifyPasswordResetDto(
    Guid UserId,
    string Code
);

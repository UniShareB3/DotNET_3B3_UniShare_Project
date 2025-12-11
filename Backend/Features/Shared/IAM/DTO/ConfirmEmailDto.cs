namespace Backend.Features.Shared.IAM.DTO;

public record ConfirmEmailDto(Guid UserId, string Code);

namespace Backend.Features.Shared.IAM.DTO;

public abstract record ConfirmEmailDto(Guid UserId, string Code);

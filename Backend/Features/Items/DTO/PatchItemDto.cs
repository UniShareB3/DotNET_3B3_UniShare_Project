namespace Backend.Features.Items.DTO;

public record PatchItemDto(
    string? ItemName,
    string? Description,
    string? Category,
    string? Condition,
    string? BlobName
);


namespace Backend.Features.Items.DTO;

public record ItemDto
(
    Guid Id,
    string Name,
    string Description, 
    string Category,
    string Condition,
    bool IsAvailable,
    string? ImageUrl,
    
    string OwnerName
);
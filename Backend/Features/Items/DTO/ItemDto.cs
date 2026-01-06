namespace Backend.Features.Items.DTO;
public record ItemDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; } 
    public required string Category { get; init; }
    public required string Condition { get; init; }
    public bool IsAvailable { get; init; }
    public string? ImageUrl { get; init; }
    public required string OwnerName { get; init; }
    
}
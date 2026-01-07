﻿namespace Backend.Features.Items.DTO;
public record ItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; } 
    public string Category { get; init; }
    public string Condition { get; init; }
    public bool IsAvailable { get; init; }
    public string? ImageUrl { get; init; }
    public Guid OwnerId { get; init; }
    public string OwnerName { get; init; }
    
}
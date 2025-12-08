
namespace Backend.Features.Items.DTO;

public record PostItemDto(Guid OwnerId,string Name,string Description,string Category,string Condition,string?ImageUrl);
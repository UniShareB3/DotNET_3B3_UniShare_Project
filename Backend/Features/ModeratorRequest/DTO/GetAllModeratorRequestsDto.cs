using Backend.Features.ModeratorRequest.Enums;

namespace Backend.Features.ModeratorRequest.DTO;

public class GetAllModeratorRequestsDto
{
    public Guid? UserId { get; set; }
    public String ? Reason { get; set; }
}
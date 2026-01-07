namespace Backend.Features.ModeratorAssignment.DTO;

public class GetAllModeratorAssignmentsDto
{
    public Guid? UserId { get; set; }
    public String ? Reason { get; set; }
}
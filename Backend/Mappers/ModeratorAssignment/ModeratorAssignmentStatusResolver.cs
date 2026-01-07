using AutoMapper;
using Backend.Features.ModeratorAssignment.Enums;

namespace Backend.Mappers.ModeratorAssignment;

public abstract class ModeratorAssignmentStatusResolver : IValueResolver<Data.ModeratorAssignment, object, string>
{
    public string Resolve(Data.ModeratorAssignment source, object destination, string destMember, ResolutionContext context)
    {
        return source.Status switch
        {
            ModeratorAssignmentStatus.Pending => "PENDING",
            ModeratorAssignmentStatus.Accepted => "ACCEPTED",
            ModeratorAssignmentStatus.Rejected => "REJECTED",
            _ => "UNKNOWN"
        };
    }
}

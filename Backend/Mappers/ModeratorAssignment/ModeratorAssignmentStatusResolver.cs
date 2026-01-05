using AutoMapper;
using Backend.Features.ModeratorAssignment.Enums;

namespace Backend.Mappers.ModeratorAssignment;

public class ModeratorAssignmentStatusResolver : IValueResolver<Data.ModeratorAssignment, object, string>
{
    public string Resolve(Data.ModeratorAssignment source, object destination, string destMember, ResolutionContext context)
    {
        return source.Status switch
        {
            ModeratorAssignmentStatus.PENDING => "PENDING",
            ModeratorAssignmentStatus.ACCEPTED => "ACCEPTED",
            ModeratorAssignmentStatus.REJECTED => "REJECTED",
            _ => "UNKNOWN"
        };
    }
}

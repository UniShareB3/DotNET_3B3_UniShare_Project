using AutoMapper;
using Backend.Features.ModeratorRequest.Enums;

namespace Backend.Mappers.ModeratorRequest;

public class ModeratorRequestStatusResolver : IValueResolver<Data.ModeratorRequest, object, string>
{
    public string Resolve(Data.ModeratorRequest source, object destination, string destMember, ResolutionContext context)
    {
        return source.Status switch
        {
            ModeratorRequestStatus.PENDING => "PENDING",
            ModeratorRequestStatus.ACCEPTED => "ACCEPTED",
            ModeratorRequestStatus.REJECTED => "REJECTED",
            _ => "UNKNOWN"
        };
    }
}


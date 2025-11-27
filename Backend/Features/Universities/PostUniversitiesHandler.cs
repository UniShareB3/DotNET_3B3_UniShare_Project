using AutoMapper;
using Backend.Data;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Universities;

public class PostUniversitiesHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<PostUniversitiesRequest, IResult>
{
    public Task<IResult> Handle(PostUniversitiesRequest request, CancellationToken cancellationToken)
    {
        University university = mapper.Map<University>(request.PostUniversityDto);
        university.Id = Guid.NewGuid();
        university.CreatedAt = DateTime.UtcNow;
        dbContext.Set<University>().Add(university);
        return dbContext.SaveChangesAsync(cancellationToken)
            .ContinueWith<IResult>(t => Results.Created("/universities", university), cancellationToken);
    }
}
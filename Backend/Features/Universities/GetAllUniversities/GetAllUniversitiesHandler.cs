using AutoMapper;
using Backend.Data;
using Backend.Features.Universities.DTO;
using Backend.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Universities;

public class GetAllUniversitiesHandler(ApplicationContext dbContext, IMapper mapper) : IRequestHandler<GetAllUniversitiesRequest, IResult>
{
    public Task<IResult> Handle(GetAllUniversitiesRequest request, CancellationToken cancellationToken)
    {
        var universities = dbContext.Set<University>()
            .AsNoTracking()
            .Select(u => mapper.Map<UniversityDto>(u));
        
        return Task.FromResult(Results.Ok(universities));
    }
}
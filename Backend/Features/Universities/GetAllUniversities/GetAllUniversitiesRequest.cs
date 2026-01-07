using MediatR;

namespace Backend.Features.Universities.GetAllUniversities;

public record GetAllUniversitiesRequest() : IRequest<IResult>;
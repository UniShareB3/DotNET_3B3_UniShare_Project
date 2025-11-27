using MediatR;

namespace Backend.Features.Universities;

public record GetAllUniversitiesRequest() : IRequest<IResult>;
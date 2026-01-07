using Backend.Features.Universities.DTO;
using MediatR;

namespace Backend.Features.Universities.PostUniversities;

public record PostUniversitiesRequest(PostUniversityDto PostUniversityDto) : IRequest<IResult>;
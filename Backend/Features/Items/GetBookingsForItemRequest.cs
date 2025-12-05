using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace Backend.Features.Items
{
    public record GetBookingsForItemRequest(Guid ItemId) : IRequest<IResult>;
}


using MediatR;

namespace Application.Mediator.Admin.Queries.GetUserDetails;

public record GetUserDetailsQuery(Guid UserId) : IRequest<GetUserDetailsResult>;
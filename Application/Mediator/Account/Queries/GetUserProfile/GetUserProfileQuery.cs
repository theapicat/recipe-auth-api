using MediatR;

namespace Application.Mediator.Account.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<GetUserProfileResult>;
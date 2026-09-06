using Domain.DTOs.Admin;
using MediatR;

namespace Application.Mediator.Admin.Queries.GetUsers;

public record GetUsersQuery() : IRequest<List<AdminUserListItemDto>>;
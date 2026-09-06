using Domain.DTOs.Admin;
using Domain.DTOs.Admin.Responses;
using MediatR;

namespace Application.Mediator.Admin.Queries.GetUsers;

public record GetUsersQuery : IRequest<List<AdminUserListItemResponse>>;
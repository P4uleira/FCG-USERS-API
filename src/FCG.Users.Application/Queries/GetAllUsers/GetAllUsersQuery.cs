using MediatR;

namespace FCG.Users.Application.Queries.GetAllUsers;

public sealed record GetAllUsersQuery
    : IRequest<IReadOnlyCollection<UserResponse>>;
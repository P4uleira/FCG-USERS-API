using FCG.Users.Application.DTOs;
using MediatR;

namespace FCG.Users.Application.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserResponse?>;
using FCG.Users.Domain.Enums;
using MediatR;

namespace FCG.Users.Application.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Name,
    string Email,
    string Password,
    UserRole Role = UserRole.User
) : IRequest<Guid>;
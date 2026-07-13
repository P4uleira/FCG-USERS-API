using MediatR;

namespace FCG.Users.Application.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(
    string Email,
    string Password
) : IRequest<AuthenticationResult>;
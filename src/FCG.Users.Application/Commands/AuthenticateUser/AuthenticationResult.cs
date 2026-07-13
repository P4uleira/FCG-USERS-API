namespace FCG.Users.Application.Commands.AuthenticateUser;

public sealed record AuthenticationResult(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    Guid UserId,
    string Name,
    string Email,
    string Role
);
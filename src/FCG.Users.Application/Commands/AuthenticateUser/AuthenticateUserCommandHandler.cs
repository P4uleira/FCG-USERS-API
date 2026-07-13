using FCG.Users.Application.Abstractions.Security;
using FCG.Users.Application.Exceptions;
using FCG.Users.Application.Settings;
using FCG.Users.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FCG.Users.Application.Commands.AuthenticateUser;

public sealed class AuthenticateUserCommandHandler
    : IRequestHandler<AuthenticateUserCommand, AuthenticationResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthenticateUserCommandHandler> _logger;

    public AuthenticateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthenticateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<AuthenticationResult> Handle(
        AuthenticateUserCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Tentativa de autenticação para o e-mail {Email}",
            normalizedEmail);

        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning(
                "Falha de autenticação para o e-mail {Email}",
                normalizedEmail);

            throw new InvalidCredentialsException();
        }

        var accessToken = _tokenService.GenerateToken(user);

        _logger.LogInformation(
            "Autenticação concluída com sucesso. UserId: {UserId}, Email: {Email}, Role: {Role}",
            user.Id,
            user.Email.Address,
            user.Role);

        return new AuthenticationResult(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: _jwtSettings.ExpirationMinutes * 60,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email.Address,
            Role: user.Role.ToString()
        );
    }
}
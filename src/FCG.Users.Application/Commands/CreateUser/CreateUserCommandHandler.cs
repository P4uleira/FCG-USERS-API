using FCG.Users.Application.Abstractions.Security;
using FCG.Users.Contracts.Events;
using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Interfaces.Repositories;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FCG.Users.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPublishEndpoint publishEndpoint,
        IPasswordHasher passwordHasher,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _publishEndpoint = publishEndpoint;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Guid> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "Já existe um usuário cadastrado com este e-mail.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Name,
            normalizedEmail,
            passwordHash,
            request.Role
        );

        await _userRepository.AddAsync(user, cancellationToken);

        await _publishEndpoint.Publish(
            new UserCreatedEvent(
                user.Id,
                user.Name,
                user.Email.Address,
                user.Role.ToString(),
                user.CreatedAt
            ),
            cancellationToken
        );

        _logger.LogInformation(
            "Usuário criado com sucesso. UserId: {UserId}, Email: {Email}, Role: {Role}",
            user.Id,
            user.Email.Address,
            user.Role);

        return user.Id;
    }
}
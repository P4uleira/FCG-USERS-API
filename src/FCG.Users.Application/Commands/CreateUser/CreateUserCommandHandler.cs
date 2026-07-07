using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Interfaces.Repositories;
using MediatR;
using FCG.Users.Contracts.Events;
using MassTransit;

namespace FCG.Users.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateUserCommandHandler(IUserRepository userRepository, IPublishEndpoint publishEndpoint)
    {
        _userRepository = userRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser is not null)
            throw new InvalidOperationException("Já existe um usuário cadastrado com este e-mail.");

        var user = User.Create(
            request.Name,
            request.Email,
            request.Password,
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

        return user.Id;
    }
}
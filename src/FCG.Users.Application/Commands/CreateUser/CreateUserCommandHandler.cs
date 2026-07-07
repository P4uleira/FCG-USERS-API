using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Interfaces.Repositories;
using MediatR;

namespace FCG.Users.Application.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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

        return user.Id;
    }
}
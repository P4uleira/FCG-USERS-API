using FCG.Users.Application.DTOs;
using FCG.Users.Domain.Interfaces.Repositories;
using MediatR;

namespace FCG.Users.Application.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserResponse?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user is null)
            return null;

        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email.Address,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}
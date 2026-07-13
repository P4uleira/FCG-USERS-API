using FCG.Users.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FCG.Users.Application.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, IReadOnlyCollection<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(
        IUserRepository userRepository,
        ILogger<GetAllUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<UserResponse>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        var response = users
            .Select(user => new UserResponse(
                user.Id,
                user.Name,
                user.Email.Address,
                user.Role.ToString(),
                user.CreatedAt))
            .ToList()
            .AsReadOnly();

        _logger.LogInformation(
            "Consulta administrativa de usuários concluída. Total: {TotalUsers}",
            response.Count);

        return response;
    }
}
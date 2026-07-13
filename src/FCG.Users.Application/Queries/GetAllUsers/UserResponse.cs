namespace FCG.Users.Application.Queries.GetAllUsers;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt
);
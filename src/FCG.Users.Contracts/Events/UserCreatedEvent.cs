namespace FCG.Users.Contracts.Events;

public sealed record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt
);
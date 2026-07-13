using FCG.Users.Domain.Enums;
using FCG.Users.Domain.ValueObjects;

namespace FCG.Users.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User()
    {
        Name = string.Empty;
        Email = null!;
        PasswordHash = string.Empty;
    }

    private User(string name, Email email, string passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email!;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(
    string name,
    string email,
    string passwordHash,
    UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Senha é obrigatória.");

        return new User(
            name.Trim(),
            Email.Create(email),
            passwordHash,
            role
        );
    }
}
using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Enums;

namespace FCG.Users.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Create_ShouldCreateUser_WhenDataIsValid()
    {
        var user = User.Create(
            name: "Usuario Teste",
            email: "Teste@email.com",
            passwordHash: "hashed-password",
            role: UserRole.User
        );

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Usuario Teste", user.Name);
        Assert.Equal("Teste@email.com", user.Email.Address);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenNameIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            User.Create("", "Teste@email.com", "hashed-password"));

        Assert.Equal("Nome é obrigatório.", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenEmailIsInvalid()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            User.Create("Usuario Teste", "email-invalido", "hashed-password"));

        Assert.Equal("E-mail inválido.", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenPasswordHashIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            User.Create("Usuario Teste", "Teste@email.com", ""));

        Assert.Equal("Senha é obrigatória.", exception.Message);
    }
}
using FCG.Users.Domain.Entities;

namespace FCG.Users.Application.Abstractions.Security;

public interface ITokenService
{
    string GenerateToken(User user);
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FCG.Users.Application.Abstractions.Security;
using FCG.Users.Application.Settings;
using FCG.Users.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FCG.Users.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        IOptions<JwtSettings> options,
        ILogger<JwtTokenService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        ValidateSettings();

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email.Address),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(now).ToString(),
                ClaimValueTypes.Integer64)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials
        );

        var serializedToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        _logger.LogInformation(
            "Token JWT gerado para UserId {UserId}, Email {Email} e Role {Role}",
            user.Id,
            user.Email.Address,
            user.Role);

        return serializedToken;
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Key) ||
            _settings.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "A chave JWT deve possuir no mínimo 32 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Issuer))
            throw new InvalidOperationException("Jwt:Issuer não foi configurado.");

        if (string.IsNullOrWhiteSpace(_settings.Audience))
            throw new InvalidOperationException("Jwt:Audience não foi configurado.");

        if (_settings.ExpirationMinutes <= 0)
            throw new InvalidOperationException(
                "Jwt:ExpirationMinutes deve ser maior que zero.");
    }
}
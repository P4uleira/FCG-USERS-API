using FCG.Users.Application.Abstractions.Security;
using FCG.Users.Application.Settings;
using FCG.Users.Domain.Interfaces.Repositories;
using FCG.Users.Infrastructure.Data;
using FCG.Users.Infrastructure.Repositories;
using FCG.Users.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FCG.Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);

        var jwtSettings = new JwtSettings
        {
            Key = jwtSection["Key"] ?? string.Empty,
            Issuer = jwtSection["Issuer"] ?? string.Empty,
            Audience = jwtSection["Audience"] ?? string.Empty,
            ExpirationMinutes =
                int.TryParse(
                    jwtSection["ExpirationMinutes"],
                    out var expirationMinutes)
                    ? expirationMinutes
                    : 60
        };

        services.AddSingleton<IOptions<JwtSettings>>(
            Options.Create(jwtSettings));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
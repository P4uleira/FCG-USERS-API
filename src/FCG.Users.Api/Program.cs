using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FCG.Users.Api.Middlewares;
using FCG.Users.Application;
using FCG.Users.Application.Settings;
using FCG.Users.Infrastructure;
using FCG.Users.Infrastructure.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

#region Controllers

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

#endregion

#region Swagger

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FCG Users API",
        Version = "v1",
        Description =
            "Microsserviço de usuários, autenticação e autorização da FIAP Cloud Games."
    });

    options.AddSecurityDefinition(
        "bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description =
                "Informe somente o token JWT. O Swagger adicionará automaticamente o prefixo Bearer.",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "bearer",
                document)] = []
        });
});

#endregion

#region Application

builder.Services.AddApplication();

#endregion

#region Infrastructure

builder.Services.AddInfrastructure(
    builder.Configuration);

#endregion

#region Authentication

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "As configurações JWT não foram encontradas.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) ||
    jwtSettings.Key.Length < 32)
{
    throw new InvalidOperationException(
        "A chave JWT deve possuir no mínimo 32 caracteres.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key)),

                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

#endregion

#region MassTransit

builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig =
            builder.Configuration.GetSection("RabbitMq");

        var rabbitMqHost =
            rabbitMqConfig["Host"]
            ?? throw new InvalidOperationException(
                "A configuração RabbitMq:Host não foi encontrada.");

        var rabbitMqUsername =
            rabbitMqConfig["Username"]
            ?? throw new InvalidOperationException(
                "A configuração RabbitMq:Username não foi encontrada.");

        var rabbitMqPassword =
            rabbitMqConfig["Password"]
            ?? throw new InvalidOperationException(
                "A configuração RabbitMq:Password não foi encontrada.");

        cfg.Host(
            rabbitMqHost,
            "/",
            host =>
            {
                host.Username(rabbitMqUsername);
                host.Password(rabbitMqPassword);
            });
    });
});

#endregion

var app = builder.Build();

#region Database Migration

await ApplyMigrationsAsync<UsersDbContext>(
    app,
    "UsersAPI");

#endregion

#region Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

var isRunningInContainer =
    string.Equals(
        Environment.GetEnvironmentVariable(
            "DOTNET_RUNNING_IN_CONTAINER"),
        "true",
        StringComparison.OrdinalIgnoreCase);

if (!isRunningInContainer)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        service = "UsersAPI",
        status = "Healthy"
    }))
    .AllowAnonymous();

#endregion

app.Run();

static async Task ApplyMigrationsAsync<TContext>(
    WebApplication app,
    string serviceName)
    where TContext : DbContext
{
    const int maxAttempts = 10;
    var retryDelay = TimeSpan.FromSeconds(5);

    var logger = app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await using var scope =
                app.Services.CreateAsyncScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<TContext>();

            logger.LogInformation(
                "Aplicando migrations do {ServiceName}. Tentativa {Attempt}/{MaxAttempts}.",
                serviceName,
                attempt,
                maxAttempts);

            await dbContext.Database.MigrateAsync();

            logger.LogInformation(
                "Migrations do {ServiceName} aplicadas com sucesso.",
                serviceName);

            return;
        }
        catch (Exception exception)
        {
            if (attempt == maxAttempts)
            {
                logger.LogCritical(
                    exception,
                    "Não foi possível aplicar as migrations do {ServiceName} após {MaxAttempts} tentativas.",
                    serviceName,
                    maxAttempts);

                throw;
            }

            logger.LogWarning(
                exception,
                "Falha ao aplicar migrations do {ServiceName}. Nova tentativa em {RetrySeconds} segundos.",
                serviceName,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay);
        }
    }
}
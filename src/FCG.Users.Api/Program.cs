using FCG.Users.Application;
using FCG.Users.Infrastructure;
using FCG.Users.Api.Middlewares;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

#region Controllers

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

#endregion

#region Application

    builder.Services.AddApplication();

#endregion

#region Infrastructure

    builder.Services.AddInfrastructure(builder.Configuration);

#endregion

#region MassTransit
    builder.Services.AddMassTransit(config =>
    {
        config.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMqConfig = builder.Configuration.GetSection("RabbitMq");

            cfg.Host(rabbitMqConfig["Host"], "/", h =>
            {
                h.Username(rabbitMqConfig["Username"]!);
                h.Password(rabbitMqConfig["Password"]!);
            });
        });
    });
#endregion

var app = builder.Build();

#region Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();
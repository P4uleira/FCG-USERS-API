using System.Net;
using System.Text.Json;
using FCG.Users.Application.Exceptions;

namespace FCG.Users.Api.Middlewares;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidCredentialsException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.Unauthorized,
                logAsWarning: true);
        }
        catch (ArgumentException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.NotFound);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.Unauthorized,
                logAsWarning: true);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(
                context,
                ex,
                HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        bool logAsWarning = false)
    {
        if (logAsWarning)
        {
            _logger.LogWarning(
                "Falha na requisição {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "Erro na requisição {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            statusCode = context.Response.StatusCode,
            message = exception.Message,
            detail = _environment.IsDevelopment()
                ? exception.StackTrace
                : null
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}
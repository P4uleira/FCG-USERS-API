using FCG.Users.Application.Commands.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Users.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "UsersAPI",
            status = "Running"
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Health), new { id = userId }, new
        {
            id = userId,
            message = "Usuário criado com sucesso."
        });
    }
}
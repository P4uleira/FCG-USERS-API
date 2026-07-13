using FCG.Users.Application.Commands.CreateUser;
using FCG.Users.Application.Queries.GetAllUsers;
using FCG.Users.Application.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Users.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
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
            service = "FCG.Users.Api",
            status = "Healthy"
        });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result },
            new { id = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(
            new GetUserByIdQuery(id),
            cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "Usuário não encontrado."
            });
        }

        return Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<UserResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(
            new GetAllUsersQuery(),
            cancellationToken);

        return Ok(users);
    }
}
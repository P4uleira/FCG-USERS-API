using FluentValidation;

namespace FCG.Users.Application.Commands.AuthenticateUser;

public sealed class AuthenticateUserCommandValidator
    : AbstractValidator<AuthenticateUserCommand>
{
    public AuthenticateUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("E-mail é obrigatório.")
            .EmailAddress()
            .WithMessage("E-mail inválido.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Senha é obrigatória.");
    }
}
using FluentValidation;

namespace FCG.Users.Application.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório.")
            .MaximumLength(150)
            .WithMessage("Nome deve ter no máximo 150 caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("E-mail é obrigatório.")
            .EmailAddress()
            .WithMessage("E-mail inválido.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Senha é obrigatória.")
            .MinimumLength(6)
            .WithMessage("Senha deve ter no mínimo 6 caracteres.");
    }
}
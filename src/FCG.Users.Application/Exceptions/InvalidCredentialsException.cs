namespace FCG.Users.Application.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("E-mail ou senha inválidos.")
    {
    }
}
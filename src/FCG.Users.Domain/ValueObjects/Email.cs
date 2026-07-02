using System.Text.RegularExpressions;

namespace FCG.Users.Domain.ValueObjects;

public sealed class Email
{
    public string Address { get; private set; }

    private Email(string address)
    {
        Address = address;
    }

    public static Email Create(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("E-mail é obrigatório.");

        address = address.Trim();

        if (!Regex.IsMatch(address, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException("E-mail inválido.");

        return new Email(address);
    }

    public override string ToString() => Address;
}
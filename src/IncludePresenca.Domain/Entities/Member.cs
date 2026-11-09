using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Domain.Entities;

public class Member
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public StatusConta AccountStatus { get; private set; }
    public Role Role { get; private set; }

    private Member()
    {
        Name = null!;
        Email = null!;
    }

    public Member(string name, string email, Role role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (!Enum.IsDefined(role))
            throw new ArgumentException("Invalid access role.", nameof(role));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = null;
        AccountStatus = StatusConta.PendenteAtivacao;
        Role = role;
    }

    public void ActivateAccount(string passwordHash)
    {
        if (AccountStatus != StatusConta.PendenteAtivacao)
            throw new InvalidOperationException("Account is not pending activation.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        AccountStatus = StatusConta.Ativa;
    }

    public void Deactivate()
    {
        if (AccountStatus == StatusConta.Desativada)
            throw new InvalidOperationException("Member is already deactivated.");

        AccountStatus = StatusConta.Desativada;
    }

    public void Reactivate()
    {
        if (AccountStatus != StatusConta.Desativada)
            throw new InvalidOperationException("Member is not deactivated.");

        AccountStatus = PasswordHash is null
            ? StatusConta.PendenteAtivacao
            : StatusConta.Ativa;
    }

    public void ResetPassword(string passwordHash)
    {
        if (AccountStatus != StatusConta.Ativa)
            throw new InvalidOperationException("Account is not active.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }
}
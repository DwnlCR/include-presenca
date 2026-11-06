using IncludePresenca.Domain.Enums;

namespace IncludePresenca.Domain.Entities;

public class Membro
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? SenhaHash {get; private set; }
    public StatusConta StatusConta {get; private set; }
    public Role Role {get; private set; }


    private Membro()
    {
        Name = null!;
        Email = null!;
    }

    public Membro(string name, string email, Role role)
    {

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome é obrigatório.", nameof(name));


        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O e-mail é obrigatorio.", nameof(email));

        if (!Enum.IsDefined(role))
            throw new ArgumentException("Perfil de acesso inválido", nameof(role));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        SenhaHash = null;
        StatusConta = StatusConta.PendenteAtivacao;
        Role = role;
    }

    public void AtivarConta(string senhaHash)
    {
        if (StatusConta != StatusConta.PendenteAtivacao)
            throw new InvalidOperationException("A conta não está pendente de ativação.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new ArgumentException("O hash da senha é obrigatório.", nameof(senhaHash));

        SenhaHash = senhaHash;
        StatusConta = StatusConta.Ativa;
    }

    public void Desativar()
    {
        if(StatusConta == StatusConta.Desativada)
            throw new InvalidOperationException("O membro ja está desativado.");

        StatusConta = StatusConta.Desativada;
    }

    public void Reativar()
    {
        if (StatusConta != StatusConta.Desativada)
            throw new InvalidOperationException("O membro não está desativado.");

        StatusConta = SenhaHash is null
            ? StatusConta.PendenteAtivacao
            : StatusConta.Ativa;
    }
}
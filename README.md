# IncludePresença

API REST desenvolvida em **C# com ASP.NET Core** para gerenciamento de membros e controle de presença em eventos da **Include Jr**.

O projeto tem como objetivo centralizar o cadastro de membros, a organização de eventos, o registro de presenças e o acompanhamento da frequência dos integrantes da empresa júnior.

O backend está sendo desenvolvido de forma incremental, com separação de responsabilidades entre as camadas de domínio, aplicação, infraestrutura e apresentação.

## Tecnologias utilizadas

- **C# / .NET 10**
- **ASP.NET Core Web API**
- **Entity Framework Core 10**
- **PostgreSQL 16**
- **Docker e Docker Compose**
- **JWT (JSON Web Token)**
- **Argon2id** para hash de senhas
- **MailKit** para envio de e-mails via SMTP
- **OpenAPI**
- **Git e GitHub**

## Arquitetura

O projeto utiliza uma arquitetura em camadas, buscando manter as regras de negócio independentes dos detalhes de infraestrutura.

```text
include-presenca/
├── src/
│   ├── IncludePresenca.Api/
│   │   ├── Controllers/
│   │   ├── ExceptionHandlers/
│   │   └── Program.cs
│   │
│   ├── IncludePresenca.Application/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   ├── Interfaces/
│   │   └── UseCases/
│   │
│   ├── IncludePresenca.Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   │
│   └── IncludePresenca.Infrastructure/
│       ├── Email/
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── Migrations/
│       │   ├── Repositories/
│       │   └── Seed/
│       └── Security/
│
├── docker-compose.yml
└── IncludePresenca.sln
```

### Domain

Contém as entidades, enumerações e regras de negócio fundamentais do sistema.

Entidades implementadas:

- `Member`
- `ActivationToken`
- `PasswordResetToken`

### Application

Responsável pela execução dos casos de uso, contratos de serviços, interfaces de repositórios e objetos de transferência de dados (DTOs).

### Infrastructure

Contém as implementações de persistência, segurança, integração com SMTP e inicialização dos dados.

### Api

Responsável por disponibilizar os endpoints HTTP, configurar as dependências, autenticação, autorização e tratamento global de exceções.

## Funcionalidades

### Gerenciamento de membros

O cadastro de membros é realizado exclusivamente por usuários com perfil de Diretoria.

Ao cadastrar um membro:

1. A Diretoria informa os dados do integrante.
2. O sistema cria a conta com status pendente de ativação.
3. Um token de ativação é gerado.
4. Um convite é enviado ao e-mail cadastrado.
5. O membro acessa o link recebido e define sua senha.
6. A conta passa a ter status ativo.

O token de ativação possui validade de **24 horas**.

Também está implementado o reenvio de convites de ativação.

### Autenticação e autorização

A autenticação é realizada utilizando JWT.

O processo de login verifica:

- Existência do usuário.
- Status ativo da conta.
- Existência de senha cadastrada.
- Correspondência entre a senha informada e o hash armazenado.

As senhas são armazenadas utilizando **Argon2id**, evitando o armazenamento de senhas em texto puro.

O token JWT possui validade de **1 hora**.

#### Perfis de acesso

| Perfil | Descrição |
|---|---|
| `Membro` | Integrante cadastrado no sistema |
| `Diretoria` | Usuário com permissão para gerenciar membros |

#### Status das contas

| Status | Descrição |
|---|---|
| `PendenteAtivacao` | Conta aguardando ativação |
| `Ativa` | Conta habilitada para autenticação |
| `Desativada` | Conta desativada |

### Recuperação de senha

O sistema permite que membros com contas ativas solicitem a redefinição de senha por e-mail.

O fluxo funciona da seguinte maneira:

1. O membro solicita a recuperação informando seu e-mail.
2. O sistema gera um token de recuperação.
3. Um link é enviado por e-mail.
4. O membro informa o token e uma nova senha.
5. O sistema valida e consome o token.
6. A nova senha é armazenada utilizando Argon2id.

Medidas implementadas:

- Token com validade de **30 minutos**.
- Tokens armazenados em formato de hash.
- Utilização única do token de recuperação.
- Invalidação de tokens anteriores durante uma nova solicitação válida.
- Intervalo de 60 segundos entre solicitações para a mesma conta.
- Limitação de 5 requisições por minuto por endereço IP no endpoint de solicitação.
- Resposta sem confirmação da existência do e-mail para contas inexistentes ou inativas.

A atualização da senha e o consumo do token são realizados dentro de uma transação.

### Tratamento de exceções

A API possui tratamento global de exceções utilizando `ProblemDetails`.

São tratados erros relacionados a:

- Dados inválidos.
- Credenciais incorretas.
- Recursos não encontrados.
- Conflitos de negócio.
- Erros internos da aplicação.

As respostas seguem um formato padronizado para facilitar a integração com o frontend.

## Endpoints

### Autenticação

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| POST | `/api/auth/login` | Autenticação do usuário | Público |
| POST | `/api/auth/activate` | Ativação da conta | Público |
| POST | `/api/auth/forgot-password` | Solicitação de recuperação de senha | Público |
| POST | `/api/auth/reset-password` | Redefinição de senha | Público |

### Membros

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| POST | `/api/members` | Cadastro de membro | Diretoria |
| POST | `/api/members/{id}/resend-activation` | Reenvio de convite de ativação | Diretoria |

As rotas de gerenciamento de membros exigem autenticação JWT com o perfil `Diretoria`.

## Configuração do ambiente

### Pré-requisitos

Para executar o backend localmente, é necessário possuir:

- .NET SDK 10
- Docker e Docker Compose
- Git
- Uma conta SMTP configurada para envio de e-mails

### 1. Clonar o repositório

```bash
git clone https://github.com/DwnlCR/include-presenca.git
cd include-presenca
```

### 2. Iniciar o PostgreSQL

O projeto possui um arquivo `docker-compose.yml` responsável pela inicialização do banco PostgreSQL.

```bash
docker compose up -d
```

O serviço utiliza:

- PostgreSQL 16
- Banco `include_presenca`
- Porta local `5432`
- Volume persistente para os dados

### 3. Configurar os segredos da aplicação

A aplicação utiliza configurações externas para conexão com o banco, autenticação JWT, envio de e-mails e criação do administrador inicial.

Na pasta `src/IncludePresenca.Api`, inicialize os User Secrets:

```bash
dotnet user-secrets init
```

Configure as seguintes propriedades com os valores do seu ambiente:

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Jwt:Issuer
Jwt:Audience

Smtp:Host
Smtp:Port
Smtp:Email
Smtp:Password

Frontend:BaseUrl

AdminSeed:Name
AdminSeed:Email
AdminSeed:Password
```

Exemplo de configuração da conexão local:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=include_presenca;Username=postgres;Password=SUA_SENHA"
```

Exemplo de configuração JWT:

```bash
dotnet user-secrets set "Jwt:Key" "SUA_CHAVE_SECRETA_FORTE"
dotnet user-secrets set "Jwt:Issuer" "IncludePresenca"
dotnet user-secrets set "Jwt:Audience" "IncludePresenca"
```

Para os serviços de e-mail, configure um servidor SMTP compatível com conexão SSL na porta informada.

O endereço `Frontend:BaseUrl` será utilizado na construção dos links de ativação e recuperação de senha.

### 4. Restaurar as dependências

Na raiz do projeto:

```bash
dotnet restore IncludePresenca.sln
```

### 5. Aplicar as migrations

```bash
dotnet ef database update \
  --project src/IncludePresenca.Infrastructure \
  --startup-project src/IncludePresenca.Api
```

Caso a ferramenta do Entity Framework ainda não esteja instalada:

```bash
dotnet tool install --global dotnet-ef
```

### 6. Executar a API

```bash
dotnet run --project src/IncludePresenca.Api
```

No ambiente de desenvolvimento configurado, a API estará disponível em:

```text
http://localhost:5045
```

## Administrador inicial

O projeto possui um mecanismo de inicialização automática de um usuário com perfil `Diretoria`.

Durante a inicialização em ambiente de desenvolvimento, o `AdminSeeder` verifica se existem membros cadastrados.

Caso a tabela esteja vazia, um administrador é criado utilizando as configurações:

```text
AdminSeed:Name
AdminSeed:Email
AdminSeed:Password
```

A senha é armazenada utilizando Argon2id.

O mecanismo não recria o administrador quando já existem membros cadastrados.

## Persistência de dados

O sistema utiliza PostgreSQL com Entity Framework Core.

A persistência está organizada em repositórios e configurações de mapeamento, com controle das alterações no esquema do banco por meio de migrations.

Tabelas implementadas:

- `Membros`
- `ActivationTokens`
- `PasswordResetTokens`

O projeto também utiliza uma abstração de unidade de trabalho (`IUnitOfWork`) para operações de persistência e execução transacional.

## Validações realizadas

Durante o desenvolvimento do módulo de identidade e autenticação, foram realizadas verificações manuais dos principais fluxos.

| Funcionalidade | Resultado |
|---|---|
| Login do administrador | Validado |
| Cadastro de membro pela Diretoria | Validado |
| Envio do convite de ativação | Validado |
| Ativação de conta | Validado |
| Login de membro ativo | Validado |
| Solicitação de recuperação de senha | Validado |
| Envio do e-mail de recuperação | Validado |
| Redefinição de senha | Validado |
| Reutilização de token consumido | Rejeitada |
| Login com senha antiga após redefinição | Rejeitado |
| Login com nova senha | Validado |

As verificações foram realizadas localmente por meio de requisições HTTP e inspeção dos registros da aplicação.

Essas verificações não substituem uma suíte de testes automatizados.

## Roadmap

### Identidade e autenticação

- [x] Estrutura inicial da solução
- [x] Configuração do PostgreSQL
- [x] Configuração do Entity Framework Core
- [x] Migrations iniciais
- [x] Entidade de membros
- [x] Cadastro de membros pela Diretoria
- [x] Criação do administrador inicial
- [x] Ativação de contas por e-mail
- [x] Reenvio de convites
- [x] Autenticação JWT
- [x] Autorização baseada em perfis
- [x] Hash de senhas com Argon2id
- [x] Recuperação de senha por e-mail
- [x] Tokens de recuperação de uso único
- [x] Tratamento global de exceções
- [x] Limitação de requisições na solicitação de recuperação

### Próximas etapas

- [ ] Implementação do módulo de eventos
- [ ] Inscrição automática de membros elegíveis em eventos
- [ ] Registro de presença e ausência
- [ ] Controle de justificativas
- [ ] Cálculo de frequência
- [ ] Relatórios de participação
- [ ] Integração com o frontend
- [ ] Ampliação da cobertura de testes

## Status do projeto

**Em desenvolvimento — MVP**

O módulo inicial de identidade e autenticação está implementado e teve seus principais fluxos verificados localmente.

Os módulos de eventos, presença e relatórios fazem parte das próximas etapas do desenvolvimento.

---

Desenvolvido para a **Include Jr**.

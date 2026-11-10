
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
- `Event`
- `EventParticipation`

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
| `Diretoria` | Usuário com permissão para gerenciar membros e eventos |

#### Status das contas

| Status | Descrição |
|---|---|
| `PendenteAtivacao` | Conta aguardando ativação |
| `Ativa` | Conta habilitada para autenticação |
| `Desativada` | Conta desativada |

### Administração de membros

Além do cadastro e do reenvio de convites, a Diretoria pode:

- Listar membros cadastrados.
- Consultar um membro por ID.
- Atualizar nome, e-mail e perfil.
- Desativar contas.
- Reativar contas.

O sistema impede a desativação ou a remoção do perfil de Diretoria do último diretor ativo.

A reativação de uma conta com senha cadastrada restaura seu status ativo. Contas sem senha permanecem pendentes de ativação.

### Gerenciamento de eventos

A Diretoria pode criar, editar e cancelar eventos. Membros autenticados podem listar eventos e consultar seus detalhes.

Cada evento possui:

- Título.
- Descrição opcional.
- Horário de início.
- Horário de término.
- Indicador de cancelamento.

As datas devem ser informadas em UTC, e o horário de término deve ser posterior ao início.

#### Inscrição automática

Ao criar um evento, o sistema inscreve automaticamente todos os membros com conta ativa, incluindo a Diretoria.

Contas pendentes de ativação ou desativadas não são elegíveis para a inscrição automática.

A criação do evento e de suas participações ocorre em uma transação.

A combinação de evento e membro possui restrição de unicidade para impedir inscrições duplicadas.

#### Cancelamento de eventos

O cancelamento é lógico, utilizando a propriedade `IsCancelled`.

O evento e suas participações permanecem armazenados após o cancelamento.

Eventos cancelados não podem ser editados ou cancelados novamente.

A inscrição automática não representa confirmação de presença. A confirmação ocorre pelo módulo de Check-in, e a apuração é realizada pelos módulos de Frequência e Relatórios.

### Check-in e controle de presenças

O sistema permite gerar um código temporário para confirmação de presença em um evento.

Regras implementadas:

- O código possui exatamente 6 dígitos numéricos.
- O código é gerado com fonte criptograficamente segura.
- Apenas o hash SHA-256 do código é armazenado no banco.
- O código expira 15 minutos após sua geração.
- Uma nova geração renova e substitui o código anterior do evento.
- Não é possível gerar código ou registrar presença em evento cancelado.
- A geração do código e a atualização manual das presenças exigem perfil `Diretoria`.
- O check-in exige autenticação e participação elegível do membro no evento.
- O código precisa existir, estar dentro da validade e corresponder ao valor informado.
- Uma participação já finalizada não pode receber outro check-in.
- O endpoint de check-in aceita no máximo 10 tentativas por minuto por endereço IP.
- O check-in é marcado como atrasado quando ocorre mais de 10 minutos após o início do evento.

As presenças podem ser consultadas pela Diretoria, e a Diretoria também pode classificá-las manualmente como presença, falta justificada ou falta não justificada.

### Frequência

A frequência individual é calculada pela fórmula:

```text
presenças / (presenças + faltas não justificadas) × 100
```

Faltas justificadas não entram no denominador. O percentual é `null` quando não existem presenças nem faltas não justificadas válidas para o cálculo.

O histórico de participações:

- pode ser consultado pelo próprio usuário autenticado;
- pode receber o filtro opcional `dias`, que deve ser maior que zero;
- exclui eventos cancelados;
- exclui eventos que ainda não terminaram;
- exclui participações ainda pendentes;
- informa presença, falta justificada, falta não justificada e atraso.

### Relatórios de frequência e presença

A Diretoria pode consultar:

- relatório global de frequência;
- relatório de frequência por membro;
- presenças, faltas justificadas, faltas não justificadas e atrasos;
- quantidade de membros ativos;
- membros abaixo da meta atualmente implementada de 75%.

O percentual de 75% é uma regra atualmente implementada para o indicador `abaixoDaMeta` e ainda depende de confirmação da equipe como meta oficial do produto.

O relatório por membro também pode ser exportado em CSV UTF-8. O exportador escapa aspas e vírgulas, preserva valores nulos e prefixa valores que poderiam ser interpretados como fórmulas por planilhas.

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
| GET | `/api/members` | Listagem de membros | Diretoria |
| GET | `/api/members/{id}` | Consulta de membro por ID | Diretoria |
| PUT | `/api/members/{id}` | Atualização de membro | Diretoria |
| PATCH | `/api/members/{id}/deactivate` | Desativação de conta | Diretoria |
| PATCH | `/api/members/{id}/reactivate` | Reativação de conta | Diretoria |

As rotas de gerenciamento de membros exigem autenticação JWT com o perfil `Diretoria`.

### Eventos

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| GET | `/api/events` | Listagem de eventos | Autenticado |
| GET | `/api/events/{id}` | Consulta de evento por ID | Autenticado |
| POST | `/api/events` | Criação de evento e inscrição automática dos membros ativos | Diretoria |
| PUT | `/api/events/{id}` | Atualização de evento | Diretoria |
| PATCH | `/api/events/{id}/cancel` | Cancelamento lógico de evento | Diretoria |

### Check-in e presenças

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| GET | `/api/eventos/{eventoId}/presencas` | Consulta das presenças do evento | Diretoria |
| POST | `/api/eventos/{eventoId}/codigos-checkin` | Geração ou renovação do código de check-in | Diretoria |
| POST | `/api/eventos/{eventoId}/checkin` | Registro de check-in do membro autenticado | Membro autenticado |
| PATCH | `/api/eventos/{eventoId}/presencas/{membroId}` | Atualização manual da situação da participação | Diretoria |

### Frequência

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| GET | `/api/me/frequencia` | Resumo da frequência do usuário autenticado | Usuário autenticado |
| GET | `/api/me/presencas?dias={n}` | Histórico de participações, opcionalmente filtrado por dias | Usuário autenticado |

### Relatórios

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| GET | `/api/relatorios/frequencia-global` | Relatório global de frequência | Diretoria |
| GET | `/api/relatorios/presencas` | Relatório de presenças e frequência por membro | Diretoria |
| GET | `/api/relatorios/presencas.csv` | Exportação do relatório por membro em CSV | Diretoria |

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

- PostgreSQL 16.
- Banco `include_presenca`.
- Porta local `5432`.
- Volume persistente para os dados.

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

### 7. Testar Check-in, Frequência e Relatórios

Os testes manuais devem usar a base de desenvolvimento `include_presenca` somente quando a intenção for validar o ambiente local. Para os testes automatizados de integração, utilize uma base PostgreSQL separada, configurada pela variável de ambiente `INCLUDE_PRESENCA_TEST_CONNECTION`. Não use a base de testes para o acesso manual ao site e não exponha a string de conexão, senhas ou outros segredos.

Para iniciar a API:

```bash
dotnet run --project src/IncludePresenca.Api --launch-profile http
```

A API ficará disponível em `http://localhost:5045`. Com um JWT válido, o arquivo [src/IncludePresenca.Api/IncludePresenca.Api.http](src/IncludePresenca.Api/IncludePresenca.Api.http) contém exemplos para consultar presenças, gerar o código de check-in, registrar presença e atualizar manualmente uma participação.

Fluxo manual recomendado:

1. Autentique uma conta com perfil `Diretoria`.
2. Crie ou selecione um evento não cancelado com participações elegíveis.
3. Gere o código em `/api/eventos/{eventoId}/codigos-checkin`.
4. Use uma conta membro autenticada para chamar `/api/eventos/{eventoId}/checkin` com o código recebido.
5. Consulte `/api/me/frequencia` e `/api/me/presencas` com o membro.
6. Consulte `/api/relatorios/frequencia-global`, `/api/relatorios/presencas` ou baixe `/api/relatorios/presencas.csv` com a Diretoria.

Para executar os testes automatizados:

```bash
dotnet test IncludePresenca.sln
```

Os testes de integração PostgreSQL só devem ser executados quando `INCLUDE_PRESENCA_TEST_CONNECTION` apontar para uma base de testes separada da base de desenvolvimento. A suíte não deve receber credenciais diretamente no README, no código ou no histórico do Git.

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
- `Eventos`
- `ParticipacoesEventos`
- `CodigosCheckin`

As participações de eventos também armazenam o status (`Pending`, `Present`, `JustifiedAbsence` ou `UnjustifiedAbsence`), o instante do check-in em UTC, a justificativa e o instante da justificativa. A tabela `CodigosCheckin` mantém um único código por evento, armazenado como hash, com os instantes UTC de criação e expiração.

O projeto também utiliza uma abstração de unidade de trabalho (`IUnitOfWork`) para operações de persistência e execução transacional.

As migrations `AddEventParticipationAttendanceFields` e `AddEventCheckInCodes` adicionam os dados de presença e a estrutura dos códigos de check-in.

A migration `AddEventsAndParticipations` adiciona as tabelas de eventos e participações, com chaves estrangeiras para os membros e restrição de unicidade por evento e participante.

## Validações realizadas

Durante o desenvolvimento dos módulos de identidade, gerenciamento de membros e eventos, foram realizadas verificações manuais dos principais fluxos.

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
| Listagem e consulta de membros | Validado |
| Atualização, desativação e reativação de membros | Validado |
| Proteção contra desativação ou rebaixamento do último diretor ativo | Validado |
| Criação de evento | Validado (`201 Created`) |
| Listagem e consulta de eventos | Validado |
| Edição de evento | Validado (`200 OK`) |
| Cancelamento lógico de evento | Validado (`204 No Content`) |
| Bloqueio de edição de evento cancelado | Validado (`409 Conflict`) |
| Inscrição automática de membros ativos, incluindo a Diretoria | Validado no cenário de três participantes |
| Preservação das três participações após cancelamento | Validado |
| Consulta de eventos por Membro | Validado (`200 OK`) |
| Bloqueio de cancelamento por Membro | Validado (`403 Forbidden`) |
| Geração e renovação de código de check-in | Validado |
| Expiração e validação do código de check-in | Validado |
| Registro de presença por membro autenticado | Validado |
| Consulta e atualização manual de presenças pela Diretoria | Validado |
| Cálculo de frequência individual e histórico de participações | Validado |
| Relatórios global e por membro | Validado |
| Exportação do relatório de presenças em CSV | Validado |

As verificações foram realizadas localmente por meio de requisições HTTP, consultas ao PostgreSQL e inspeção dos registros da aplicação.

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

### Gerenciamento de membros

- [x] Cadastro de membros pela Diretoria
- [x] Listagem e consulta de membros
- [x] Atualização de membros
- [x] Desativação e reativação de contas
- [x] Proteção do último diretor ativo

### Eventos

- [x] Entidades e persistência de eventos e participações
- [x] Criação, listagem e consulta de eventos
- [x] Edição e cancelamento lógico de eventos
- [x] Inscrição automática de membros ativos, incluindo a Diretoria
- [x] Autorização por perfil nos endpoints de eventos

### Próximas etapas

- [x] Registro de presença e ausência
- [ ] Controle de justificativas
- [x] Cálculo de frequência
- [x] Relatórios de participação
- [x] Integração entre os módulos de Check-in, Frequência e Relatórios
- [ ] Integração com o frontend
- [ ] Ampliação da cobertura de testes

## Status do projeto

**Em desenvolvimento — MVP**

Os módulos de identidade, autenticação, gerenciamento de membros e gerenciamento de eventos estão implementados, com seus principais fluxos validados localmente.

A criação de eventos realiza a inscrição automática dos membros ativos, incluindo integrantes da Diretoria. Os módulos de Check-in, Frequência e Relatórios estão implementados no backend e possuem testes automatizados e validações locais.

A integração completa com o frontend ainda não deve ser considerada concluída. O controle de justificativas e a ampliação da cobertura de testes permanecem como próximas etapas.

---

Desenvolvido para a **Include Jr**.

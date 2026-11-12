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
- **xUnit** para testes automatizados
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
├── tests/
│   └── IncludePresenca.Application.Tests/
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
- `EventParticipationAudit`

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

### Resumo de eventos

O sistema disponibiliza um endpoint para consulta de indicadores gerais dos eventos, acessível exclusivamente pela Diretoria.

O resumo apresenta:

- **Próximo evento:** título e data de início do evento futuro mais próximo.
- **Eventos no mês:** quantidade de eventos não cancelados no mês atual.
- **Presença média:** percentual de presenças nas participações finalizadas dos eventos encerrados.

#### Regras de cálculo

O próximo evento considera apenas eventos não cancelados cuja data de início seja posterior ao instante atual.

A quantidade de eventos no mês considera o mês e o ano atuais no fuso horário `America/Sao_Paulo`, desconsiderando eventos cancelados.

A presença média considera somente participações de eventos não cancelados que já terminaram.

Participações com status `Pending` são desconsideradas.

A fórmula utilizada é:

```text
presença média = (participações presentes / participações finalizadas) × 100
```

O cálculo considera os seguintes status:

| Status | Considerado no cálculo |
|---|---|
| `Pending` | Não |
| `Present` | Sim, como presença |
| `JustifiedAbsence` | Sim, no denominador |
| `UnjustifiedAbsence` | Sim, no denominador |

O percentual é arredondado para duas casas decimais.

A presença média é calculada sobre o conjunto de participações finalizadas elegíveis, e não pela média aritmética dos percentuais individuais de cada evento.

Quando não existem participações finalizadas elegíveis, o valor retornado é `null`.

Quando não existe evento futuro elegível, o próximo evento também é `null`.

#### Exemplo de resposta

```json
{
  "proximoEvento": {
    "titulo": "Reunião Geral Include Jr",
    "inicioEm": "2026-11-20T14:00:00Z"
  },
  "eventosNoMes": 1,
  "presencaMedia": null
}
```

O resumo utiliza os dados persistidos no PostgreSQL, sem depender de valores simulados.

### Check-in e controle de presenças

O Check-in vincula uma participação elegível ao membro autenticado pelo JWT. A Diretoria gera um código numérico de seis dígitos; somente o hash SHA-256 é persistido, o código expira em 15 minutos e uma nova geração substitui o anterior.

Regras implementadas:

- código gerado com fonte criptograficamente segura;
- evento cancelado, evento fora do período permitido, código inexistente ou expirado rejeitam o check-in;
- participação finalizada não pode ser registrada novamente;
- limite de 10 tentativas por minuto por IP no endpoint de check-in;
- check-in realizado mais de 10 minutos após o início é marcado como atraso;
- consulta e correção manual exigem `Diretoria`, com auditoria na correção.

Os status de presença aceitos na correção administrativa são `PRESENTE`, `FALTA_JUSTIFICADA` e `FALTA_NAO_JUSTIFICADA` (também são aceitos os nomes internos em inglês). Algumas respostas de presença ainda serializam os nomes internos `Present`, `JustifiedAbsence` e `UnjustifiedAbsence`; o frontend deve tratar essa diferença explicitamente.

### Correção administrativa de presenças

O sistema permite que a Diretoria corrija uma participação já finalizada.

A correção administrativa exige:

- Participação existente.
- Status anterior finalizado.
- Novo status válido e finalizado.
- Justificativa administrativa obrigatória.
- Dados adicionais compatíveis com o novo status.

O sistema impede correções que não produzam uma alteração efetiva na participação.

As correções são registradas em auditoria, preservando informações como:

- Status anterior e novo status.
- Dados anteriores e novos da participação.
- Identificação do diretor responsável.
- Motivo administrativo da alteração.
- Data e horário da correção em UTC.

A atualização da participação e o registro de auditoria são realizados em uma transação, evitando alterações parciais.

### Frequência

A frequência individual é calculada como `presenças / (presenças + faltas não justificadas) × 100`. Faltas justificadas não entram no denominador e o percentual é `null` quando não há ocorrências válidas para o cálculo.

O histórico individual é isolado pelo membro autenticado, aceita o filtro opcional `dias` (inteiro maior que zero), exclui eventos cancelados ou ainda não encerrados e participações pendentes, e informa `PRESENTE`, `ATRASO`, `FALTA_JUSTIFICADA` ou `FALTA_NAO_JUSTIFICADA`. A aprovação de uma justificativa altera a participação e, consequentemente, os indicadores de frequência.

**Observação:** a fórmula de frequência individual é diferente da fórmula utilizada no resumo geral de eventos. Na frequência individual, faltas justificadas não entram no denominador. Na presença média dos eventos, elas são consideradas entre as participações finalizadas.

### Relatórios de frequência e presença

Os relatórios são exclusivos da `Diretoria` e consolidam apenas dados persistidos de participações elegíveis. O relatório global informa totais, membros ativos e membros abaixo da meta atualmente implementada de 75%; o relatório por membro informa presenças, faltas, atrasos e percentual. O relatório por membro também pode ser exportado em CSV UTF-8, com escaping de campos e proteção contra fórmulas de planilhas.

### Justificativas

Um membro pode justificar uma falta não justificada dentro do prazo de 48 horas após o encerramento do evento. O envio exige motivo, descrição entre 50 e 2.000 caracteres e, opcionalmente, anexo privado PDF, PNG ou JPEG de até 5 MB. Não são permitidas solicitações pendentes ou aprovadas duplicadas para a mesma falta.

A Diretoria consulta a fila e decide `APROVADA` ou `REJEITADA`. A aprovação converte a participação para falta justificada, atualiza a frequência e registra auditoria; ambas as decisões são auditadas.

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
| GET | `/api/events/summary` | Resumo dos eventos e presença média | Diretoria |
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

### Justificativas

| Método | Endpoint | Descrição | Acesso |
|---|---|---|---|
| GET | `/api/me/faltas?semJustificativa={bool}` | Lista faltas elegíveis para justificativa; o filtro é opcional | Usuário autenticado |
| GET | `/api/me/justificativas` | Lista as justificativas do próprio membro | Usuário autenticado |
| POST | `/api/me/justificativas` | Envia uma justificativa em `multipart/form-data` | Usuário autenticado |
| GET | `/api/justificativas` | Consulta a fila de justificativas | Diretoria |
| PATCH | `/api/justificativas/{id}` | Aprova ou rejeita uma justificativa | Diretoria |

Todas as rotas protegidas exigem `Authorization: Bearer <JWT>`. Os identificadores `{eventoId}`, `{membroId}`, `{faltaId}` e `{id}` são GUIDs.

#### Payloads e respostas principais

Geração e check-in:

```http
POST /api/eventos/{eventoId}/codigos-checkin
Authorization: Bearer <JWT-DIRETORIA>
```

```json
POST /api/eventos/{eventoId}/checkin
Authorization: Bearer <JWT-MEMBRO>
Content-Type: application/json

{ "codigo": "123456" }
```

O check-in retorna `200 OK` com `participacaoId`, `status`, `checkedInAtUtc` e `isLate`. A geração retorna `200 OK` com o código em texto e sua expiração; falhas de validação retornam `400`, falta de autenticação `401`, falta de permissão `403`, conflito de participação `409` e excesso de tentativas `429`.

Correção administrativa:

```http
PATCH /api/eventos/{eventoId}/presencas/{membroId}
Authorization: Bearer <JWT-DIRETORIA>
Content-Type: application/json

{ "status": "FALTA_JUSTIFICADA", "justification": "Motivo administrativo" }
```

Essa operação retorna `200 OK`. A correção auditada usa `PATCH /api/eventos/{eventoId}/presencas/{membroId}/correcao`, exige `administrativeReason` e retorna `204 No Content`.

Envio de justificativa:

```text
POST /api/me/justificativas
Content-Type: multipart/form-data

FaltaId: <GUID>
Motivo: SAUDE | TRABALHO_ESTUDOS | PROBLEMAS_TECNICOS | OUTRO
Descricao: texto com 50 a 2000 caracteres
Anexo: arquivo opcional PDF, PNG ou JPEG (até 5 MB)
```

O envio retorna `201 Created`; duplicidade, falta inelegível ou fora do prazo retornam `409 Conflict`, e arquivo ou campos inválidos retornam `400 Bad Request`. A decisão usa:

```json
{ "status": "APROVADA" }
```

ou `{ "status": "REJEITADA" }` e retorna `204 No Content`.

As respostas de frequência usam os campos `totalEventos`, `presencas`, `faltasJustificadas`, `faltasNaoJustificadas`, `atrasos` e `percentual`. O histórico usa GUIDs para `id` e `eventoId`; `GET /api/me/presencas` retorna `200 OK` e aceita `dias` maior que zero.

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

Para verificar as migrations aplicadas:

```bash
dotnet ef migrations list \
  --project src/IncludePresenca.Infrastructure \
  --startup-project src/IncludePresenca.Api
```

Para verificar alterações pendentes no modelo:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/IncludePresenca.Infrastructure \
  --startup-project src/IncludePresenca.Api
```

### 6. Executar a API

```bash
dotnet run --project src/IncludePresenca.Api
```

No ambiente de desenvolvimento configurado, a API estará disponível em:

```text
http://localhost:5045
```

### 7. Testar os eventos e o resumo

Com a API em execução, autentique uma conta com perfil `Diretoria`:

```bash
curl -X POST http://localhost:5045/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "EMAIL_DIRETORIA",
    "password": "SENHA_DIRETORIA"
  }'
```

Com o `accessToken` retornado, configure a variável de ambiente no terminal:

```bash
TOKEN="SEU_ACCESS_TOKEN"
```

Para criar um evento:

```bash
curl -i -X POST http://localhost:5045/api/events \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Reunião Geral Include Jr",
    "description": "Reunião de acompanhamento dos projetos",
    "startsAt": "2026-11-20T14:00:00Z",
    "endsAt": "2026-11-20T16:00:00Z"
  }'
```

As datas acima são apenas exemplos e devem ser ajustadas conforme o cenário de teste.

Para consultar o resumo:

```bash
curl -s http://localhost:5045/api/events/summary \
  -H "Authorization: Bearer $TOKEN" | jq
```

A consulta exige um JWT válido com perfil `Diretoria`.

### 8. Testar Check-in, Frequência e Relatórios

Os testes manuais devem usar a base de desenvolvimento `include_presenca` somente quando a intenção for validar o ambiente local.

Para os testes automatizados de integração, utilize uma base PostgreSQL separada, configurada pela variável de ambiente `INCLUDE_PRESENCA_TEST_CONNECTION`.

Não use a base de testes para o acesso manual ao site e não exponha a string de conexão, senhas ou outros segredos.

Para iniciar a API:

```bash
dotnet run --project src/IncludePresenca.Api --launch-profile http
```

A API ficará disponível em `http://localhost:5045`.

Com um JWT válido, o arquivo [src/IncludePresenca.Api/IncludePresenca.Api.http](src/IncludePresenca.Api/IncludePresenca.Api.http) contém exemplos para consultar presenças, gerar o código de check-in, registrar presença e atualizar manualmente uma participação.

Fluxo manual recomendado:

1. Autentique uma conta com perfil `Diretoria`.
2. Crie ou selecione um evento não cancelado com participações elegíveis.
3. Gere o código em `/api/eventos/{eventoId}/codigos-checkin`.
4. Use uma conta membro autenticada para chamar `/api/eventos/{eventoId}/checkin` com o código recebido.
5. Consulte `/api/me/frequencia` e `/api/me/presencas` com o membro.
6. Consulte `/api/relatorios/frequencia-global`, `/api/relatorios/presencas` ou baixe `/api/relatorios/presencas.csv` com a Diretoria.

Para justificar uma falta, use o membro autenticado em `/api/me/faltas`, envie o formulário multipart para `/api/me/justificativas` e depois decida a solicitação com a conta de `Diretoria` em `/api/justificativas/{id}`.

Para executar os testes automatizados:

```powershell
$env:INCLUDE_PRESENCA_TEST_CONNECTION="Host=localhost;Port=5433;Database=include_presenca_migrations_test;Username=postgres;Password=SENHA_LOCAL_DO_BANCO_DE_TESTE"
dotnet test tests/IncludePresenca.Application.Tests/IncludePresenca.Application.Tests.csproj --no-restore --verbosity minimal
```

No PowerShell, use `$env:INCLUDE_PRESENCA_TEST_CONNECTION` com a senha definida localmente; no Bash, exporte a mesma variável com `export`. O valor acima é apenas um modelo e não é uma credencial do projeto.

Antes da suíte PostgreSQL, confirme que o container é o ambiente isolado esperado e que a porta local está acessível:

```powershell
docker ps --filter "name=^include_presenca_test_db$"
docker exec include_presenca_test_db pg_isready -U postgres -d include_presenca_migrations_test
docker exec include_presenca_test_db psql -U postgres -d include_presenca_migrations_test -c "SELECT current_database();"
```

O banco de testes deve ser `include_presenca_migrations_test` no container `include_presenca_test_db`, publicado em `localhost:5433`, e deve permanecer separado do banco de desenvolvimento `include_presenca` em `localhost:5432`. Não execute os testes apontando para produção ou para a base de desenvolvimento.

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
- `SolicitacoesJustificativas`
- `AuditoriaSolicitacoesJustificativas`

O sistema também possui persistência de auditoria das correções administrativas de presença, por meio da entidade `EventParticipationAudit`.

As participações de eventos armazenam:

- Identificação do evento.
- Identificação do membro.
- Status da participação.
- Instante do check-in em UTC.
- Justificativa de ausência.
- Instante da justificativa em UTC.

Os status disponíveis são:

- `Pending`
- `Present`
- `JustifiedAbsence`
- `UnjustifiedAbsence`

A tabela `CodigosCheckin` mantém um único código por evento, armazenado como hash, com os instantes UTC de criação e expiração.

As solicitações de justificativa armazenam o membro, a participação, o motivo, a descrição, o status, os metadados do anexo e os instantes de envio e decisão. O conteúdo dos anexos é mantido em armazenamento privado; a API não expõe caminho físico nem permite acesso sem autorização. As decisões também são registradas em `AuditoriaSolicitacoesJustificativas`.

O projeto também utiliza uma abstração de unidade de trabalho (`IUnitOfWork`) para operações de persistência e execução transacional.

### Migrations

As principais migrations implementadas são:

| Migration | Descrição |
|---|---|
| `InitialIdentity` | Estrutura inicial de identidade |
| `AddActivationTokens` | Tokens de ativação |
| `AddPasswordResetTokens` | Tokens de recuperação de senha |
| `AddPasswordResetTokenCreatedAt` | Data de criação dos tokens de recuperação |
| `AddEventsAndParticipations` | Eventos e participações |
| `AddEventParticipationAttendanceFields` | Campos de presença e justificativa |
| `AddEventCheckInCodes` | Códigos temporários de check-in |
| `AddEventParticipationAudits` | Auditoria de correções administrativas |

A migration `AddEventsAndParticipations` adiciona as tabelas de eventos e participações, com chaves estrangeiras para os membros e restrição de unicidade por evento e participante.

A migration `AddEventParticipationAttendanceFields` adiciona os campos necessários para registrar o resultado das participações.

A migration `AddEventCheckInCodes` adiciona a estrutura de armazenamento dos códigos de check-in.

A migration `AddEventParticipationAudits` adiciona a persistência dos registros de auditoria das correções administrativas.

O módulo de resumo de eventos utiliza as estruturas existentes e não necessita de uma migration específica.

## Validações realizadas

Durante o desenvolvimento, foram realizadas verificações manuais dos principais fluxos.

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
| Isolamento e autorização da frequência individual | Validado |
| Envio multipart, validação e armazenamento privado de anexos | Validado |
| Bloqueio de justificativas duplicadas e fora do prazo | Validado |
| Aprovação/rejeição, auditoria e atualização da frequência | Validado |
| Consulta do resumo de eventos pela Diretoria | Validado (`200 OK`) |
| Identificação do próximo evento futuro | Validado |
| Contagem de eventos no mês | Validado |
| Retorno de `null` para presença média sem participações finalizadas elegíveis | Validado |
| Cálculo da presença média com participações finalizadas | Implementado, pendente de validação manual específica |

As verificações foram realizadas localmente por meio de requisições HTTP, consultas ao PostgreSQL isolado e inspeção dos registros da aplicação. A suíte automatizada validada no ambiente de teste PostgreSQL aprovou 206 testes, com 0 falhas e 0 ignorados. Isso comprova os fluxos cobertos pela suíte; não representa integração completa com o frontend.

### Testes automatizados

A solução possui uma suíte de testes automatizados utilizando xUnit. A validação mais recente aprovou 206 testes, com 0 falhas e 0 ignorados, incluindo os fluxos HTTP, PostgreSQL e integração disponíveis no projeto. As verificações manuais e automatizadas são complementares.

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
- [x] Endpoint de resumo de eventos
- [x] Identificação do próximo evento
- [x] Contagem de eventos no mês
- [x] Implementação do cálculo de presença média
- [x] Validação manual da consulta e dos indicadores de próximo evento e eventos no mês
- [ ] Validação manual da presença média com participações finalizadas

### Check-in e frequência

- [x] Geração de códigos temporários de check-in
- [x] Validação e expiração dos códigos
- [x] Registro de presença
- [x] Atualização manual de presenças
- [x] Correção administrativa de participações finalizadas
- [x] Auditoria das correções administrativas
- [x] Cálculo de frequência individual
- [x] Histórico de participações
- [x] Relatórios de participação
- [x] Exportação de relatórios em CSV
- [x] Integração entre os módulos de Check-in, Frequência e Relatórios

### Próximas etapas

- [x] Registro de presença e ausência
- [x] Controle de justificativas
- [x] Cálculo de frequência
- [x] Relatórios de participação
- [x] Integração entre Check-in, Frequência, Relatórios e Justificativas
- [x] Suíte automatizada com persistência PostgreSQL isolada
- [ ] Integração com o frontend
- [ ] Validação manual completa da presença média
- [ ] Integração do endpoint de resumo com o frontend
- [ ] Integração completa com o frontend
- [ ] Ampliação da cobertura de testes

## Status do projeto

**Em desenvolvimento — MVP**

Os módulos de identidade, autenticação, gerenciamento de membros e gerenciamento de eventos estão implementados, com seus principais fluxos validados localmente.

A criação de eventos realiza a inscrição automática dos membros ativos, incluindo integrantes da Diretoria. Os módulos de Check-in, Frequência, Relatórios e Justificativas estão implementados no backend, com persistência PostgreSQL, autenticação JWT, autorização por perfil e 206 testes automatizados aprovados.
A criação de eventos realiza a inscrição automática dos membros ativos, incluindo integrantes da Diretoria. O resumo de eventos também está implementado, disponibilizando informações sobre o próximo evento, a quantidade de eventos no mês e a presença média; seu endpoint `GET /api/events/summary` foi validado manualmente com autenticação da Diretoria. O cálculo de presença média permanece com validação manual específica pendente para participações finalizadas.

A integração completa com o frontend ainda não deve ser considerada concluída. Permanecem ajustes nos tipos de GUID, desativação dos mocks ativos por padrão, compatibilidade dos status e contratos do módulo de Eventos e conexão efetiva da tela de Relatórios. Esses ajustes dependem dos responsáveis pelo frontend e pelos módulos correspondentes.

---

Desenvolvido para a **Include Jr**.

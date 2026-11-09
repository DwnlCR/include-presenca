IncludePresença
API REST desenvolvida em C# com ASP.NET Core para gerenciamento de membros e controle de presença em eventos da Include Jr.

O projeto tem como objetivo centralizar o cadastro de membros, a organização de eventos, o registro de presenças e o acompanhamento da frequência dos integrantes da empresa júnior.

O backend está sendo desenvolvido de forma incremental, com separação de responsabilidades entre as camadas de domínio, aplicação, infraestrutura e apresentação.
Tecnologias utilizadas
- C# / .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL 16
- Docker e Docker Compose
- JWT (JSON Web Token)
- Argon2id para hash de senhas
- MailKit para envio de e-mails via SMTP
- OpenAPI
- Git e GitHub
Arquitetura
O projeto utiliza uma arquitetura em camadas, buscando manter as regras de negócio independentes dos detalhes de infraestrutura.
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
Domain
Contém as entidades, enumerações e regras de negócio fundamentais do sistema.

Entidades implementadas:
- Member
- ActivationToken
- PasswordResetToken
- Event
- EventParticipation
Application
Responsável pela execução dos casos de uso, contratos de serviços, interfaces de repositórios e objetos de transferência de dados (DTOs).
Infrastructure
Contém as implementações de persistência, segurança, integração com SMTP e inicialização dos dados.
Api
Responsável por disponibilizar os endpoints HTTP, configurar as dependências, autenticação, autorização e tratamento global de exceções.
Funcionalidades
Gerenciamento de membros
O cadastro de membros é realizado exclusivamente por usuários com perfil de Diretoria.

Ao cadastrar um membro:
1. A Diretoria informa os dados do integrante.
2. O sistema cria a conta com status pendente de ativação.
3. Um token de ativação é gerado.
4. Um convite é enviado ao e-mail cadastrado.
5. O membro acessa o link recebido e define sua senha.
6. A conta passa a ter status ativo.
O token de ativação possui validade de 24 horas.

Também está implementado o reenvio de convites de ativação.
Autenticação e autorização
A autenticação é realizada utilizando JWT.

O processo de login verifica:
- Existência do usuário.
- Status ativo da conta.
- Existência de senha cadastrada.
- Correspondência entre a senha informada e o hash armazenado.
As senhas são armazenadas utilizando Argon2id, evitando o armazenamento de senhas em texto puro.

O token JWT possui validade de 1 hora.
Perfis de acesso
Perfil	Descrição
Membro	Integrante cadastrado no sistema
Diretoria	Usuário com permissão para gerenciar membros e eventos


Status das contas
Status	Descrição
PendenteAtivacao	Conta aguardando ativação
Ativa	Conta habilitada para autenticação
Desativada	Conta desativada


Administração de membros
Além do cadastro e do reenvio de convites, a Diretoria pode listar membros, consultar um integrante por ID, atualizar nome, e-mail e perfil, desativar e reativar contas.

O sistema impede a desativação ou a remoção do perfil de Diretoria do último diretor ativo. A reativação de uma conta com senha cadastrada restaura seu status ativo; contas sem senha permanecem pendentes de ativação.
Gerenciamento de eventos
A Diretoria pode criar, editar e cancelar eventos. Membros autenticados podem listar eventos e consultar seus detalhes.

Cada evento possui título, descrição opcional, horário de início, horário de término e indicador de cancelamento. As datas devem ser informadas em UTC, e o horário de término deve ser posterior ao início.

Ao criar um evento, o sistema inscreve automaticamente todos os membros com conta ativa, incluindo a Diretoria. Contas pendentes de ativação ou desativadas não são elegíveis para a inscrição automática.

A criação do evento e de suas participações ocorre em uma transação. A combinação de evento e membro possui restrição de unicidade para impedir inscrições duplicadas.

O cancelamento é lógico (IsCancelled = true): o evento e suas participações permanecem armazenados. Eventos cancelados não podem ser editados ou cancelados novamente.

A inscrição automática não representa confirmação de presença. O check-in e a apuração da frequência serão integrados em módulos específicos.
Recuperação de senha
O sistema permite que membros com contas ativas solicitem a redefinição de senha por e-mail.

O fluxo funciona da seguinte maneira:
1. O membro solicita a recuperação informando seu e-mail.
2. O sistema gera um token de recuperação.
3. Um link é enviado por e-mail.
4. O membro informa o token e uma nova senha.
5. O sistema valida e consome o token.
6. A nova senha é armazenada utilizando Argon2id.
Medidas implementadas:
- Token com validade de 30 minutos.
- Tokens armazenados em formato de hash.
- Utilização única do token de recuperação.
- Invalidação de tokens anteriores durante uma nova solicitação válida.
- Intervalo de 60 segundos entre solicitações para a mesma conta.
- Limitação de 5 requisições por minuto por endereço IP no endpoint de solicitação.
- Resposta sem confirmação da existência do e-mail para contas inexistentes ou inativas.
A atualização da senha e o consumo do token são realizados dentro de uma transação.
Tratamento de exceções
A API possui tratamento global de exceções utilizando ProblemDetails.

São tratados erros relacionados a:
- Dados inválidos.
- Credenciais incorretas.
- Recursos não encontrados.
- Conflitos de negócio.
- Erros internos da aplicação.
As respostas seguem um formato padronizado para facilitar a integração com o frontend.
Endpoints
Autenticação
Método	Endpoint	Descrição	Acesso
POST	/api/auth/login	Autenticação do usuário	Público
POST	/api/auth/activate	Ativação da conta	Público
POST	/api/auth/forgot-password	Solicitação de recuperação de senha	Público
POST	/api/auth/reset-password	Redefinição de senha	Público


Membros
Método	Endpoint	Descrição	Acesso
POST	/api/members	Cadastro de membro	Diretoria
POST	/api/members/{id}/resend-activation	Reenvio de convite de ativação	Diretoria
GET	/api/members	Listagem de membros	Diretoria
GET	/api/members/{id}	Consulta de membro por ID	Diretoria
PUT	/api/members/{id}	Atualização de membro	Diretoria
PATCH	/api/members/{id}/deactivate	Desativação de conta	Diretoria
PATCH	/api/members/{id}/reactivate	Reativação de conta	Diretoria


As rotas de gerenciamento de membros exigem autenticação JWT com o perfil Diretoria.
Eventos
Método	Endpoint	Descrição	Acesso
GET	/api/events	Listagem de eventos	Autenticado
GET	/api/events/{id}	Consulta de evento por ID	Autenticado
POST	/api/events	Criação de evento e inscrição automática dos membros ativos	Diretoria
PUT	/api/events/{id}	Atualização de evento	Diretoria
PATCH	/api/events/{id}/cancel	Cancelamento lógico de evento	Diretoria


Configuração do ambiente
Pré-requisitos
Para executar o backend localmente, é necessário possuir:
- .NET SDK 10
- Docker e Docker Compose
- Git
- Uma conta SMTP configurada para envio de e-mails
1. Clonar o repositório
git clone https://github.com/DwnlCR/include-presenca.git
cd include-presenca
2. Iniciar o PostgreSQL
O projeto possui um arquivo docker-compose.yml responsável pela inicialização do banco PostgreSQL.
docker compose up -d
O serviço utiliza:
- PostgreSQL 16
- Banco include_presenca
- Porta local 5432
- Volume persistente para os dados
3. Configurar os segredos da aplicação
A aplicação utiliza configurações externas para conexão com o banco, autenticação JWT, envio de e-mails e criação do administrador inicial.

Na pasta src/IncludePresenca.Api, inicialize os User Secrets:
dotnet user-secrets init
Configure as seguintes propriedades com os valores do seu ambiente:
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
Exemplo de configuração da conexão local:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=include_presenca;Username=postgres;Password=SUA_SENHA"
Exemplo de configuração JWT:
dotnet user-secrets set "Jwt:Key" "SUA_CHAVE_SECRETA_FORTE"
dotnet user-secrets set "Jwt:Issuer" "IncludePresenca"
dotnet user-secrets set "Jwt:Audience" "IncludePresenca"
Para os serviços de e-mail, configure um servidor SMTP compatível com conexão SSL na porta informada.

O endereço Frontend:BaseUrl será utilizado na construção dos links de ativação e recuperação de senha.
4. Restaurar as dependências
Na raiz do projeto:
dotnet restore IncludePresenca.sln
5. Aplicar as migrations
dotnet ef database update \
  --project src/IncludePresenca.Infrastructure \
  --startup-project src/IncludePresenca.Api
Caso a ferramenta do Entity Framework ainda não esteja instalada:
dotnet tool install --global dotnet-ef
6. Executar a API
dotnet run --project src/IncludePresenca.Api
No ambiente de desenvolvimento configurado, a API estará disponível em:
http://localhost:5045
Administrador inicial
O projeto possui um mecanismo de inicialização automática de um usuário com perfil Diretoria.

Durante a inicialização em ambiente de desenvolvimento, o AdminSeeder verifica se existem membros cadastrados.

Caso a tabela esteja vazia, um administrador é criado utilizando as configurações:
AdminSeed:Name
AdminSeed:Email
AdminSeed:Password
A senha é armazenada utilizando Argon2id.

O mecanismo não recria o administrador quando já existem membros cadastrados.
Persistência de dados
O sistema utiliza PostgreSQL com Entity Framework Core.

A persistência está organizada em repositórios e configurações de mapeamento, com controle das alterações no esquema do banco por meio de migrations.

Tabelas implementadas:
- Membros
- ActivationTokens
- PasswordResetTokens
- Eventos
- ParticipacoesEventos
O projeto também utiliza uma abstração de unidade de trabalho (IUnitOfWork) para operações de persistência e execução transacional.

A migration AddEventsAndParticipations adiciona as tabelas de eventos e participações, com chaves estrangeiras para os membros e restrição de unicidade por evento e participante.
Validações realizadas
Durante o desenvolvimento dos módulos de identidade, gerenciamento de membros e eventos, foram realizadas verificações manuais dos principais fluxos.
Funcionalidade	Resultado
Login do administrador	Validado
Cadastro de membro pela Diretoria	Validado
Envio do convite de ativação	Validado
Ativação de conta	Validado
Login de membro ativo	Validado
Solicitação de recuperação de senha	Validado
Envio do e-mail de recuperação	Validado
Redefinição de senha	Validado
Reutilização de token consumido	Rejeitada
Login com senha antiga após redefinição	Rejeitado
Login com nova senha	Validado
Listagem e consulta de membros	Validado
Atualização, desativação e reativação de membros	Validado
Proteção contra desativação ou rebaixamento do último diretor ativo	Validado
Criação de evento	Validado (201 Created)
Listagem e consulta de eventos	Validado
Edição de evento	Validado (200 OK)
Cancelamento lógico de evento	Validado (204 No Content)
Bloqueio de edição de evento cancelado	Validado (409 Conflict)
Inscrição automática de membros ativos, incluindo a Diretoria	Validado no cenário de três participantes
Preservação das três participações após cancelamento	Validado
Consulta de eventos por Membro	Validado (200 OK)
Bloqueio de cancelamento por Membro	Validado (403 Forbidden)


As verificações foram realizadas localmente por meio de requisições HTTP, consultas ao PostgreSQL e inspeção dos registros da aplicação.

Essas verificações não substituem uma suíte de testes automatizados.
Roadmap
Identidade e autenticação
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
- [x] Listagem e consulta de membros
- [x] Atualização, desativação e reativação de membros
- [x] Proteção do último diretor ativo
Eventos
- [x] Entidades e persistência de eventos e participações
- [x] Criação, listagem e consulta de eventos
- [x] Edição e cancelamento lógico de eventos
- [x] Inscrição automática de membros ativos, incluindo a Diretoria
- [x] Autorização por perfil nos endpoints de eventos
Próximas etapas
- [ ] Registro de presença e ausência
- [ ] Controle de justificativas
- [ ] Cálculo de frequência
- [ ] Relatórios de participação
- [ ] Integração com o frontend
- [ ] Ampliação da cobertura de testes

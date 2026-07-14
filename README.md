# FCG Users API

Microsserviço responsável pelo cadastro, autenticação e consulta de usuários da plataforma **FIAP Cloud Games (FCG)**.

A aplicação utiliza **DDD**, **CQRS** e **MediatR** para separar os casos de uso de escrita e leitura. Também gera tokens **JWT**, aplica autorização baseada em roles e publica o evento `UserCreatedEvent` no RabbitMQ após a criação de um usuário.

## Responsabilidades

- Cadastrar usuários.
- Validar os dados de cadastro.
- Impedir o cadastro duplicado de e-mails.
- Armazenar senhas utilizando hash BCrypt.
- Autenticar usuários por e-mail e senha.
- Gerar tokens JWT.
- Autorizar operações de acordo com as roles `User` e `Admin`.
- Consultar o usuário autenticado.
- Consultar usuários por identificador.
- Listar usuários para administradores.
- Publicar o evento `UserCreatedEvent` no RabbitMQ.
- Disponibilizar health check para monitoramento.

## Tecnologias

- .NET 10
- ASP.NET Core Web API
- Domain-Driven Design (DDD)
- CQRS
- MediatR
- FluentValidation
- Entity Framework Core
- SQL Server
- JWT Bearer
- BCrypt
- MassTransit 8.4.1
- RabbitMQ
- Swagger / OpenAPI
- xUnit
- Docker

## Arquitetura

A solução está organizada em projetos com responsabilidades separadas:

```text
FCG-User-Api/
├── src/
│   ├── FCG.Users.Api/
│   │   ├── Controllers/
│   │   ├── Middlewares/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── FCG.Users.Application/
│   │   ├── Abstractions/
│   │   ├── Commands/
│   │   ├── DTOs/
│   │   ├── Exceptions/
│   │   ├── Queries/
│   │   └── Settings/
│   ├── FCG.Users.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Interfaces/
│   │   └── ValueObjects/
│   ├── FCG.Users.Infrastructure/
│   │   ├── Data/
│   │   ├── Repositories/
│   │   └── Security/
│   └── FCG.Users.Contracts/
│       └── Events/
├── tests/
│   └── FCG.Users.Tests/
├── Dockerfile
└── FCG.Users.slnx
```

### Fluxo de dependências

```text
HTTP Request
    ↓
Controller
    ↓
Command ou Query
    ↓
MediatR
    ↓
Handler
    ↓
Domain / Repository / Security / RabbitMQ
```

### CQRS e ausência de `UserService`

A aplicação não utiliza um serviço genérico chamado `UserService`. No padrão CQRS, cada caso de uso é representado por um comando ou consulta e executado pelo respectivo handler.

| Caso de uso | Componente |
|---|---|
| Criar usuário | `CreateUserCommandHandler` |
| Autenticar usuário | `AuthenticateUserCommandHandler` |
| Buscar usuário por ID | `GetUserByIdQueryHandler` |
| Listar usuários | `GetAllUsersQueryHandler` |

Os handlers atuam como serviços de aplicação específicos. Dessa forma, cada classe possui uma responsabilidade clara e não é necessário adicionar uma camada genérica de `UserService`.

## Domínio

### Entidade `User`

Representa o usuário da plataforma e contém:

- `Id`
- `Name`
- `Email`
- `PasswordHash`
- `Role`
- `CreatedAt`

A criação ocorre pelo método de fábrica `User.Create`, responsável por preservar as regras básicas da entidade.

### Value Object `Email`

O e-mail é representado pelo value object `Email`, que:

- rejeita valores vazios;
- remove espaços externos;
- valida o formato informado;
- encapsula o endereço na propriedade `Address`.

### Roles

| Role | Descrição |
|---|---|
| `User` | Usuário comum da plataforma. |
| `Admin` | Usuário com permissão administrativa. |

## Autenticação JWT

A autenticação é realizada por meio de JWT Bearer.

O token possui informações utilizadas pelos demais microsserviços, incluindo:

- identificador do usuário;
- nome;
- e-mail;
- role.

A API valida:

- assinatura do token;
- emissor (`Issuer`);
- audiência (`Audience`);
- tempo de expiração;
- chave JWT com no mínimo 32 caracteres.

A tolerância para diferença de relógio (`ClockSkew`) é de 30 segundos.

### Realizar login

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "usuario@fcg.com",
  "password": "123456"
}
```

Resposta esperada — `200 OK`:

```json
{
  "accessToken": "<TOKEN_JWT>",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "00000000-0000-0000-0000-000000000000",
  "name": "Usuário FCG",
  "email": "usuario@fcg.com",
  "role": "User"
}
```

Credenciais inválidas retornam `401 Unauthorized`.

### Usar o token no Swagger

1. Acesse o Swagger da UsersAPI.
2. Clique em **Authorize**.
3. Informe somente o conteúdo de `accessToken`.
4. Confirme a autorização.

O Swagger adiciona automaticamente o prefixo `Bearer`.

### Usar o token manualmente

```http
Authorization: Bearer <TOKEN_JWT>
```

### Consultar o usuário autenticado

```http
GET /api/auth/me
Authorization: Bearer <TOKEN_JWT>
```

Resposta esperada — `200 OK`:

```json
{
  "userId": "00000000-0000-0000-0000-000000000000",
  "name": "Usuário FCG",
  "email": "usuario@fcg.com",
  "role": "User"
}
```

## Endpoints

### Autenticação

| Método | Endpoint | Autorização | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/login` | Público | Autentica um usuário e retorna o token JWT. |
| `GET` | `/api/auth/me` | Autenticado | Retorna os dados presentes no token do usuário autenticado. |

### Usuários

| Método | Endpoint | Autorização | Descrição |
|---|---|---|---|
| `POST` | `/api/users` | Público | Cadastra um novo usuário. |
| `GET` | `/api/users/{id}` | Autenticado | Consulta um usuário pelo identificador. |
| `GET` | `/api/users` | `Admin` | Lista todos os usuários. |
| `GET` | `/health` | Público | Informa o estado da aplicação. |

Existe também o endpoint `/api/users/health`, herdando a exigência de autenticação do `UsersController`. Para monitoramento da infraestrutura, utilize o endpoint público `/health`.

## Cadastro de usuário

```http
POST /api/users
Content-Type: application/json
```

```json
{
  "name": "Usuário FCG",
  "email": "usuario@fcg.com",
  "password": "123456",
  "role": "User"
}
```

Resposta esperada — `201 Created`:

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

### Validações

- Nome obrigatório.
- Nome com no máximo 150 caracteres.
- E-mail obrigatório e válido.
- Senha obrigatória.
- Senha com no mínimo 6 caracteres.
- E-mail não pode estar previamente cadastrado.

O e-mail é normalizado com remoção de espaços externos e conversão para letras minúsculas antes da consulta e persistência.

## Consultar usuário por ID

```http
GET /api/users/{id}
Authorization: Bearer <TOKEN_JWT>
```

Possíveis respostas:

- `200 OK`: usuário encontrado;
- `401 Unauthorized`: token ausente ou inválido;
- `404 Not Found`: usuário não encontrado.

## Listar usuários

```http
GET /api/users
Authorization: Bearer <TOKEN_JWT_ADMIN>
```

Possíveis respostas:

- `200 OK`: lista retornada;
- `401 Unauthorized`: token ausente ou inválido;
- `403 Forbidden`: usuário autenticado sem role `Admin`.

## Evento publicado

Após persistir um novo usuário, a aplicação publica o evento `UserCreatedEvent` por meio do MassTransit.

Estrutura do evento:

```text
UserCreatedEvent
├── UserId
├── Name
├── Email
├── Role
└── CreatedAt
```

Fluxo:

```text
POST /api/users
    ↓
CreateUserCommandHandler
    ↓
Usuário persistido no SQL Server
    ↓
UserCreatedEvent publicado
    ↓
RabbitMQ
    ↓
NotificationsAPI
    ↓
Notificação de boas-vindas simulada
```

A UsersAPI apenas publica o evento. O consumo é responsabilidade da NotificationsAPI.

## Banco de dados

A persistência utiliza Entity Framework Core com SQL Server.

Contexto:

```text
UsersDbContext
```

Banco utilizado no ambiente orquestrado:

```text
FCGUsersDb
```

A solução contém a migração inicial em:

```text
src/FCG.Users.Infrastructure/Data/Migrations
```

### Aplicar as migrações manualmente

Execute a partir da raiz do repositório:

```powershell
dotnet ef database update `
  --project src/FCG.Users.Infrastructure `
  --startup-project src/FCG.Users.Api `
  --context UsersDbContext
```

A connection string deve estar configurada antes da execução.

## Configuração

O `appsettings.json` mantém somente configurações não sensíveis:

```json
{
  "Jwt": {
    "Issuer": "FCG.Users.Api",
    "Audience": "FCG.Client",
    "ExpirationMinutes": 60
  },
  "RabbitMq": {
    "Host": "localhost"
  }
}
```

Senhas, connection strings e a chave JWT devem ser fornecidas externamente.

### Variáveis de ambiente

| Variável | Obrigatória | Descrição |
|---|---:|---|
| `ConnectionStrings__DefaultConnection` | Sim | Connection string do banco SQL Server. |
| `Jwt__Key` | Sim | Chave de assinatura JWT com no mínimo 32 caracteres. |
| `Jwt__Issuer` | Sim | Emissor do token. |
| `Jwt__Audience` | Sim | Audiência do token. |
| `Jwt__ExpirationMinutes` | Não | Tempo de validade do token em minutos. O padrão é 60. |
| `RabbitMq__Host` | Sim | Host do RabbitMQ. |
| `RabbitMq__Username` | Sim | Usuário do RabbitMQ. |
| `RabbitMq__Password` | Sim | Senha do RabbitMQ. |
| `ASPNETCORE_ENVIRONMENT` | Não | Ambiente da aplicação. |

Em variáveis de ambiente do .NET, a hierarquia de configuração é representada por dois sublinhados (`__`).

### Exemplo para execução local no PowerShell

Use valores locais válidos e não publique credenciais reais no repositório:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=FCGUsersDb;User Id=sa;Password=<SQLSERVER_PASSWORD>;TrustServerCertificate=True;"
$env:Jwt__Key = "<JWT_KEY_COM_NO_MINIMO_32_CARACTERES>"
$env:Jwt__Issuer = "FCG.Users.Api"
$env:Jwt__Audience = "FCG.Client"
$env:Jwt__ExpirationMinutes = "60"
$env:RabbitMq__Host = "localhost"
$env:RabbitMq__Username = "<RABBITMQ_USER>"
$env:RabbitMq__Password = "<RABBITMQ_PASSWORD>"
```

## Executar isoladamente

### Pré-requisitos

- .NET SDK 10
- SQL Server acessível
- RabbitMQ acessível
- Ferramenta `dotnet-ef`, caso seja necessário aplicar migrações

### Restaurar dependências

```powershell
cd D:\FIAP-FCG-MICROSERVICOS\FCG-User-Api
dotnet restore FCG.Users.slnx
```

### Compilar

```powershell
dotnet build FCG.Users.slnx
```

### Configurar as variáveis

Configure as variáveis apresentadas na seção anterior antes de iniciar a aplicação.

### Aplicar migrações

```powershell
dotnet ef database update `
  --project src/FCG.Users.Infrastructure `
  --startup-project src/FCG.Users.Api `
  --context UsersDbContext
```

### Iniciar a API

```powershell
dotnet run --project src/FCG.Users.Api
```

Endereços do perfil local:

```text
HTTP:  http://localhost:5077
HTTPS: https://localhost:7255
```

Swagger em desenvolvimento:

```text
http://localhost:5077/swagger
```

O Swagger é habilitado somente quando `ASPNETCORE_ENVIRONMENT` está definido como `Development`.

## Executar com Docker

O `Dockerfile` utiliza build multi-stage com imagens oficiais do .NET 10.

### Gerar a imagem

Execute na raiz do repositório:

```powershell
docker build -t fcg-users-api:1.0 .
```

### Executar o container isoladamente

O container escuta internamente na porta `8080`.

```powershell
docker run --rm `
  --name fcg-users-api `
  -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Server=<SQLSERVER_HOST>,1433;Database=FCGUsersDb;User Id=sa;Password=<SQLSERVER_PASSWORD>;TrustServerCertificate=True;" `
  -e Jwt__Key="<JWT_KEY_COM_NO_MINIMO_32_CARACTERES>" `
  -e Jwt__Issuer="FCG.Users.Api" `
  -e Jwt__Audience="FCG.Client" `
  -e Jwt__ExpirationMinutes="60" `
  -e RabbitMq__Host="<RABBITMQ_HOST>" `
  -e RabbitMq__Username="<RABBITMQ_USER>" `
  -e RabbitMq__Password="<RABBITMQ_PASSWORD>" `
  fcg-users-api:1.0
```

Para a execução integrada com SQL Server, RabbitMQ e os demais microsserviços, utilize o repositório `FCG-Orchestration-Api`.

## Health check

Endpoint público:

```http
GET /health
```

Teste local:

```powershell
curl http://localhost:5077/health
```

Teste no ambiente Docker Compose:

```powershell
curl http://localhost:8080/health
```

Resposta esperada:

```json
{
  "service": "UsersAPI",
  "status": "Healthy"
}
```

O endpoint informa que o processo da API está ativo. Ele não executa uma validação profunda das conexões com SQL Server e RabbitMQ.

## Testes automatizados

Os testes estão no projeto:

```text
tests/FCG.Users.Tests
```

Executar todos os testes:

```powershell
dotnet test FCG.Users.slnx
```

Executar somente o projeto de testes:

```powershell
dotnet test tests/FCG.Users.Tests/FCG.Users.Tests.csproj
```

Executar com configuração Release:

```powershell
dotnet test FCG.Users.slnx --configuration Release
```

## Respostas HTTP principais

| Código | Situação |
|---:|---|
| `200 OK` | Login, consulta ou listagem realizada com sucesso. |
| `201 Created` | Usuário cadastrado com sucesso. |
| `400 Bad Request` | Dados inválidos ou regra de cadastro não atendida. |
| `401 Unauthorized` | Credenciais inválidas, token ausente, expirado ou inválido. |
| `403 Forbidden` | Usuário autenticado sem a role necessária. |
| `404 Not Found` | Usuário não encontrado. |
| `500 Internal Server Error` | Falha interna não tratada especificamente. |

## Tratamento de erros

A API utiliza `ErrorHandlingMiddleware` para centralizar o tratamento das exceções e evitar lógica repetida nos controllers.

Entre os casos tratados estão:

- argumentos inválidos;
- recurso não encontrado;
- acesso não autorizado;
- credenciais inválidas;
- falhas inesperadas.

## Logs

A aplicação utiliza `ILogger` para registrar operações relevantes, incluindo:

- tentativa de autenticação;
- falha de autenticação;
- autenticação concluída;
- criação de usuário;
- consulta administrativa de usuários;
- acesso ao endpoint `/api/auth/me`.

Exemplo para acompanhar logs no ambiente orquestrado:

```powershell
docker compose logs -f users-api
```

## Segurança

- Senhas não são persistidas em texto puro.
- O hash de senha utiliza BCrypt.
- A chave JWT deve possuir no mínimo 32 caracteres.
- Tokens possuem tempo de expiração configurável.
- O endpoint administrativo de listagem exige role `Admin`.
- Dados sensíveis não devem ser armazenados no `appsettings.json` nem enviados ao GitHub.
- Em produção, utilize segredos do ambiente de execução.

### Atenção ao cadastro de administradores

O endpoint público `POST /api/users` recebe a propriedade `role`. No estado atual, um cliente pode solicitar o cadastro com a role `Admin`.

Esse comportamento facilita a criação de usuários administrativos durante a demonstração acadêmica, mas não deve ser adotado sem controles adicionais em um ambiente real. Em produção, o cadastro público deveria criar somente usuários com role `User`, deixando a criação ou promoção de administradores para um fluxo protegido.

### HTTPS

A aplicação utiliza redirecionamento HTTPS quando executada fora de container. Dentro do container, o redirecionamento é desabilitado pela variável `DOTNET_RUNNING_IN_CONTAINER=true`, permitindo que a terminação HTTPS seja tratada pela infraestrutura externa quando aplicável.

## Troubleshooting

### Erro: configurações JWT não encontradas

Confirme se as configurações `Jwt__Issuer`, `Jwt__Audience` e `Jwt__Key` foram fornecidas.

```powershell
$env:Jwt__Key
$env:Jwt__Issuer
$env:Jwt__Audience
```

### Erro: a chave JWT deve possuir no mínimo 32 caracteres

Use uma chave com 32 ou mais caracteres:

```powershell
$env:Jwt__Key = "<JWT_KEY_COM_NO_MINIMO_32_CARACTERES>"
```

### Erro: login falhou para o SQL Server

Verifique:

- host e porta;
- usuário e senha;
- nome do banco;
- disponibilidade do SQL Server;
- valor de `ConnectionStrings__DefaultConnection`.

### Erro: Broker unreachable

Verifique:

- se o RabbitMQ está em execução;
- host configurado em `RabbitMq__Host`;
- credenciais do broker;
- rede utilizada pelos containers;
- estado de saúde do RabbitMQ.

### Erro `401 Unauthorized`

Confirme se:

- o header `Authorization` foi enviado;
- o token possui o prefixo `Bearer` fora do Swagger;
- o token não expirou;
- `Issuer`, `Audience` e `Key` são iguais entre geração e validação.

### Erro `403 Forbidden`

O token é válido, mas a role não possui acesso ao recurso. O endpoint `GET /api/users` exige `Admin`.

### E-mail já cadastrado

Utilize outro endereço de e-mail. A API não permite duplicidade.

### Swagger não abre

O Swagger é disponibilizado somente em ambiente `Development`.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/FCG.Users.Api
```

## Fluxo resumido

### Cadastro

```text
Cliente
  ↓ POST /api/users
UsersController
  ↓
CreateUserCommand
  ↓
CreateUserCommandHandler
  ├── normaliza e valida o e-mail
  ├── verifica duplicidade
  ├── gera o hash da senha
  ├── cria a entidade User
  ├── persiste no SQL Server
  └── publica UserCreatedEvent no RabbitMQ
```

### Autenticação

```text
Cliente
  ↓ POST /api/auth/login
AuthController
  ↓
AuthenticateUserCommand
  ↓
AuthenticateUserCommandHandler
  ├── localiza o usuário
  ├── valida a senha com BCrypt
  └── gera o token JWT
```

## Integração com os demais microsserviços

```text
UsersAPI
   ↓ UserCreatedEvent
RabbitMQ
   ↓
NotificationsAPI
```

O token gerado pela UsersAPI também é utilizado para autorizar operações protegidas em outros serviços, como a compra de jogos na CatalogAPI.

## Licença e finalidade

Projeto desenvolvido para o **Tech Challenge da FIAP — FIAP Cloud Games**, com finalidade acadêmica.

# MyCleanApi — Production-Ready Clean Architecture .NET 10 Starter

A robust, enterprise-grade, and reusable **.NET 10 Web API starter platform** and **cross-platform CLI tool** built on practical **Clean Architecture**, EF Core 10, JWT Bearer authentication, and modern C# 13/.NET 10 conventions.

Distributed as both a **.NET Global Tool (`MyCleanApi.Cli`)** and an official **`dotnet new` template (`MyCleanApi.Template`)**.

---

## Highlights

- **Modern .NET 10 & C# 13**: File-scoped namespaces, primary constructors, collection expressions, nullable reference types, and async I/O with `CancellationToken`.
- **Practical Clean Architecture**: Strict separation of concerns (API → Application → Domain ← Infrastructure) without over-engineering (no MediatR, CQRS, or generic repository overhead).
- **Dual Database Provider Support**: First-class support for both **Microsoft SQL Server** and **PostgreSQL (Npgsql)** with isolated provider configuration and health checks.
- **Production Authentication**:
  - Secure registration (`POST /api/auth/register`) with input and password complexity validation.
  - Secure login (`POST /api/auth/login`) with JWT token generation and salted PBKDF2-SHA512 password hashing (constant-time verification).
  - Authenticated user endpoint (`GET /api/auth/me`).
- **Standardized Error Handling**: RFC 9457 / Problem Details global exception handler with distinct HTTP status codes (200, 201, 400, 401, 403, 404, 409, 500).
- **Structured Logging**: Pre-configured Serilog console and HTTP request logging.
- **Interactive OpenAPI / Swagger**: Pre-configured with JWT Bearer authentication (`Authorize` dialog in Swagger UI).
- **Comprehensive Automated Testing**: Complete xUnit test suites covering Unit Tests and `WebApplicationFactory` Integration Tests using an isolated In-Memory database.
- **Central Package Management (CPM)**: Centralized package version management with `Directory.Packages.props` and `Directory.Build.props`.
- **True Cross-Platform CLI**: Compatible with macOS, Linux, and Windows with zero OS-specific shell assumptions.

---

## Repository Structure

```text
init-templates/
├── .github/
│   └── workflows/
│       └── ci.yml                 # Cross-platform matrix CI (Ubuntu, Windows, macOS)
├── .editorconfig                  # Central code styling rules
├── .gitignore                     # .NET build and OS ignore rules
├── Directory.Build.props          # Global compiler flags & metadata
├── Directory.Packages.props       # Central Package Management (CPM)
├── global.json                    # Pin to .NET 10 SDK
├── MyCleanApi.slnx                # Root solution
├── README.md                      # Platform documentation
│
├── src/
│   ├── MyCleanApi.Cli/            # Cross-platform CLI Global Tool
│   │   ├── Commands/              # New, Help, Version commands
│   │   ├── Common/                # Console UI, Process runner
│   │   ├── Services/              # Project generator & template installer
│   │   ├── Resources/             # Bundled template nupkg
│   │   └── Program.cs
│   │
│   └── MyCleanApi.Template/       # NuGet template package
│       ├── MyCleanApi.Template.csproj
│       └── content/               # Clean Architecture Starter Solution
│           ├── .template.config/
│           │   └── template.json  # Template engine definition
│           ├── MyCompany.MyApi.slnx
│           ├── Directory.Build.props
│           ├── Directory.Packages.props
│           │
│           ├── src/
│           │   ├── MyCompany.MyApi.Domain/
│           │   │   ├── Common/    # BaseEntity, IAuditableEntity
│           │   │   ├── Entities/  # User entity
│           │   │   └── Enums/     # UserRole
│           │   │
│           │   ├── MyCompany.MyApi.Application/
│           │   │   ├── Common/    # Custom exceptions (NotFound, Conflict, etc.)
│           │   │   ├── DTOs/      # RegisterRequest, LoginRequest, AuthResponse
│           │   │   ├── Interfaces/# IUserRepository, IPasswordHasher, IJwtTokenGenerator
│           │   │   ├── Services/  # AuthenticationService
│           │   │   ├── Validators/# FluentValidation rules
│           │   │   └── DependencyInjection.cs
│           │   │
│           │   ├── MyCompany.MyApi.Infrastructure/
│           │   │   ├── Authentication/  # PBKDF2 hasher, JWT token generator
│           │   │   ├── Persistence/     # ApplicationDbContext, Repositories, Providers
│           │   │   ├── Services/        # CurrentUserService
│           │   │   └── DependencyInjection.cs
│           │   │
│           │   └── MyCompany.MyApi.API/
│           │       ├── Controllers/     # AuthController, BaseApiController
│           │       ├── Middleware/      # GlobalExceptionHandler (Problem Details)
│           │       ├── Extensions/      # Swagger, CORS, Health checks
│           │       ├── appsettings.json
│           │       └── Program.cs
│           │
│           └── tests/
│               ├── MyCompany.MyApi.UnitTests/
│               └── MyCompany.MyApi.IntegrationTests/
│
└── tests/
    └── MyCleanApi.Cli.Tests/      # CLI unit and end-to-end generation tests
```

---

## Architectural Principles & Decisions

### 1. Unified Architecture: CLI + Template
Rather than having two distinct generators with diverged logic, the repository uses **`dotnet new` as the template engine** and wraps it in **`MyCleanApi.Cli`**:
- `MyCleanApi.Template` contains the solution files and `.template.config/template.json`.
- `MyCleanApi.Cli` embeds the template package `.nupkg` directly in its assembly.
- When `mycleanapi new` is run, the CLI ensures the template is installed in `dotnet new` (offline, without internet), invokes `dotnet new`, verifies all generated artifacts, and presents a rich terminal UI with progress steps and next instructions.
- Developers who prefer standard .NET SDK commands can use `dotnet new my-clean-api` directly.

### 2. Pragmatic Clean Architecture
Many Clean Architecture templates add unnecessary ceremony (MediatR commands/queries/handlers, event buses, unit of work wrappers over EF Core, generic repository abstractions).
Here, we follow a simple, maintainable flow:
```text
HTTP Controller
     ↓
Application Service (Business logic, orchestration, validation)
     ↓
Repository / Infrastructure (IUserRepository, DbContext, PasswordHasher, Jwt)
     ↓
Database (SQL Server / PostgreSQL)
```
- **Domain**: Pure C#. Independent of EF Core, ASP.NET Core, and third-party libraries.
- **Application**: Depends solely on Domain. Defines interfaces, DTOs, FluentValidation validators, and application services.
- **Infrastructure**: Implements interfaces defined by Application. Encapsulates EF Core DbContext, providers, migrations, and JWT cryptography.
- **API**: Thin presentation layer handling HTTP routing, OpenAPI documentation, Problem Details exception formatting, and DI composition.

### 3. Password Security & Cryptography
- Passwords are never stored in plain text.
- Passwords are hashed using **PBKDF2** with **HMAC-SHA512**, **100,000 iterations**, a cryptographically random **32-byte salt**, and a **64-byte derived key**.
- Verification uses `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.

### 4. Database Provider Isolation
Provider configuration is isolated in `Infrastructure/Persistence/DatabaseExtensions.cs`.
The application selects its provider at startup via `Database:Provider`:
```json
{
  "Database": {
    "Provider": "SqlServer"  // or "PostgreSql"
  }
}
```
During project scaffolding, the chosen provider is configured in `appsettings.json`.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.100 or later)

---

## Installation

### Option A: Install Global CLI Tool (Recommended)

```bash
dotnet tool install --global MyCleanApi.Cli
```

Verify installation:
```bash
mycleanapi --version
mycleanapi --help
```

### Option B: Install `dotnet new` Template Directly

```bash
dotnet new install MyCleanApi.Template
```

Verify installed templates:
```bash
dotnet new list my-clean-api
```

---

## Creating a New Project

### Using the CLI (`mycleanapi`)

#### 1. SQL Server Backend (Default)
```bash
mycleanapi new MyCompany.OrderingService
```

#### 2. PostgreSQL Backend
```bash
mycleanapi new MyCompany.OrderingService --database postgresql
```

#### 3. Custom Output Directory
```bash
mycleanapi new MyCompany.OrderingService -o ./services/ordering-service --database postgresql
```

### Using `dotnet new`

```bash
# SQL Server
dotnet new my-clean-api -n MyCompany.OrderingService --database sqlserver

# PostgreSQL
dotnet new my-clean-api -n MyCompany.OrderingService --database postgresql
```

---

## Running the Generated Project

Navigate to the generated directory:
```bash
cd MyCompany.OrderingService
```

Restore dependencies:
```bash
dotnet restore
```

Build the solution:
```bash
dotnet build
```

Run unit and integration tests:
```bash
dotnet test
```

Start the API:
```bash
dotnet run --project src/MyCompany.OrderingService.API
```

Once running:
- **Swagger UI**: Open `https://localhost:5001` or `http://localhost:5000` in your browser.
- **Health Check**: Visit `https://localhost:5001/health`.

---

## Authentication & API Workflow

The template includes an interactive authentication flow:

### 1. Register a User
```http
POST /api/auth/register
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "password": "Password@123"
}
```

Response (`201 Created`):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-10-04T16:00:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "firstName": "Jane",
    "lastName": "Doe",
    "email": "jane.doe@example.com",
    "role": "User",
    "createdAt": "2026-10-04T15:00:00Z"
  }
}
```

### 2. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "jane.doe@example.com",
  "password": "Password@123"
}
```

### 3. Access Protected Endpoint
```http
GET /api/auth/me
Authorization: Bearer <accessToken>
```

Response (`200 OK`):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "role": "User",
  "createdAt": "2026-10-04T15:00:00Z"
}
```

### 4. Interactive Swagger UI
1. Navigate to the Swagger UI root.
2. Call `POST /api/auth/login` or `POST /api/auth/register`.
3. Copy the returned `accessToken`.
4. Click the green **Authorize** button in Swagger UI.
5. Paste the token into the value box and click **Authorize**.
6. Execute `GET /api/auth/me` to verify claims and authorization.

---

## Database Migrations

Entity Framework Core migrations are executed via the `dotnet-ef` tool:

### Install `dotnet-ef` Tool
```bash
dotnet tool install --global dotnet-ef
```

### Add a Migration
```bash
dotnet ef migrations add InitialCreate \
  --project src/MyCompany.OrderingService.Infrastructure \
  --startup-project src/MyCompany.OrderingService.API
```

### Apply Migrations to Database
```bash
dotnet ef database update \
  --project src/MyCompany.OrderingService.Infrastructure \
  --startup-project src/MyCompany.OrderingService.API
```

---

## Configuration & Environment Management

### Local Development (`.NET User Secrets`)
Do not store production secrets in `appsettings.json`. For local development, use User Secrets:

```bash
cd src/MyCompany.OrderingService.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "YourStrongSecretKeyAtLeast32CharactersLong!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=MyDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True"
```

### Production Environment Variables
All configuration values can be overridden via environment variables:

```bash
ConnectionStrings__DefaultConnection="Server=sql.prod.internal;Database=OrdersDb;User Id=app;Password=ProdPassword;TrustServerCertificate=False"
Database__Provider="SqlServer"
Jwt__SecretKey="ProductionSecretKeyAtLeast32CharactersLong!"
Jwt__Issuer="MyCompany.OrderingService"
Jwt__Audience="MyCompany.OrderingService.Client"
Cors__AllowedOrigins__0="https://app.mycompany.com"
```

---

## Building & Packaging the Starter Toolkit

To build the entire solution and generate production `.nupkg` packages:

```bash
# 1. Clean and restore
dotnet restore MyCleanApi.slnx

# 2. Build in Release mode
dotnet build MyCleanApi.slnx -c Release --no-restore

# 3. Run all tests
dotnet test MyCleanApi.slnx -c Release --no-build

# 4. Pack Template package
dotnet pack src/MyCleanApi.Template/MyCleanApi.Template.csproj -c Release -o artifacts/

# 5. Refresh bundled resource in CLI and pack CLI tool
cp artifacts/MyCleanApi.Template.1.0.0.nupkg src/MyCleanApi.Cli/Resources/
dotnet pack src/MyCleanApi.Cli/MyCleanApi.Cli.csproj -c Release -o artifacts/
```

Generated packages will be in `./artifacts`:
- `artifacts/MyCleanApi.Template.1.0.0.nupkg`
- `artifacts/MyCleanApi.Cli.1.0.0.nupkg`

---

## Publishing to NuGet.org

To publish both packages to NuGet:

```bash
dotnet nuget push artifacts/MyCleanApi.Template.1.0.0.nupkg \
  --api-key <YOUR_NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push artifacts/MyCleanApi.Cli.1.0.0.nupkg \
  --api-key <YOUR_NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

---

## Updating & Versioning Strategy

- **Updating the CLI tool**:
  ```bash
  dotnet tool update --global MyCleanApi.Cli
  ```
- **Updating the `dotnet new` template**:
  ```bash
  dotnet new update
  ```
- **Generated projects**: Generated projects are self-contained and completely independent of the CLI. Updating the CLI or template does not affect previously generated solutions.

---

## License

MIT License. See [LICENSE](LICENSE) for details.

# WeSpace.Api

Production-ready Clean Architecture .NET 10 Web API with JWT Bearer Authentication, EF Core, FluentValidation, Serilog, and OpenAPI.

## Getting Started

### 1. Restore & Build
```bash
dotnet restore
dotnet build
```

### 2. Run Tests
```bash
dotnet test
```

### 3. Run the API
```bash
dotnet run --project src/WeSpace.Api.API
```

- **Swagger UI**: Navigate to `https://localhost:5001` (or `http://localhost:5000`)
- **Health Check**: Navigate to `https://localhost:5001/health`

## Database Setup & Migrations

### Apply Migrations
```bash
dotnet ef database update \
  --project src/WeSpace.Api.Infrastructure \
  --startup-project src/WeSpace.Api.API
```

### Add New Migration
```bash
dotnet ef migrations add <MigrationName> \
  --project src/WeSpace.Api.Infrastructure \
  --startup-project src/WeSpace.Api.API
```

## Configuration

Update `src/WeSpace.Api.API/appsettings.json` or configure environment variables:

- `ConnectionStrings:DefaultConnection`
- `Database:Provider` (`SqlServer` or `PostgreSql`)
- `Jwt:SecretKey` (Minimum 32 characters)
- `Jwt:Issuer`
- `Jwt:Audience`
- `Cors:AllowedOrigins`

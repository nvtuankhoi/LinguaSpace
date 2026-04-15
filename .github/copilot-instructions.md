# Copilot Instructions for LinguaSpace

## Build, Run & Test

```bash
# Build
dotnet build

# Run (starts Aspire dashboard + PostgreSQL container + Web API)
dotnet run --project src\AppHost

# Run all tests
dotnet test

# Run a single test project
dotnet test tests\Application.UnitTests
dotnet test tests\Domain.UnitTests
dotnet test tests\Application.FunctionalTests
dotnet test tests\Infrastructure.IntegrationTests

# Run a single test by name
dotnet test tests\Application.FunctionalTests --filter "CreateTodoItemTests"
```

The app runs via .NET Aspire (`src\AppHost`). The Aspire dashboard opens automatically. The Web API is exposed with a Scalar API Reference at `/scalar`.

## Architecture

This is a Clean Architecture solution based on the [Jason Taylor template](https://github.com/jasontaylordev/CleanArchitecture) (v10.8.0). Dependency flow: **Domain → Application → Infrastructure / Web**.

| Project | Role |
|---|---|
| `Domain` | Entities, value objects, domain events, enums — no external dependencies |
| `Application` | MediatR commands/queries, validators, interfaces (`IApplicationDbContext`), AutoMapper profiles, pipeline behaviours |
| `Infrastructure` | EF Core `ApplicationDbContext`, ASP.NET Identity, EF interceptors, DI registration |
| `Web` | Minimal API endpoints (`IEndpointGroup`), OpenAPI/Scalar config, auth middleware |
| `AppHost` | .NET Aspire orchestration — wires PostgreSQL container + Web project |
| `Shared` | Shared constants (`Services.cs`) referenced by both `AppHost` and other projects |

## Key Conventions

### MediatR Command/Query pattern
Every use case lives in `src\Application\{Feature}/Commands/{Name}/` or `.../Queries/{Name}/`. Each folder contains:
- `{Name}.cs` — the `record` command/query implementing `IRequest<T>` and the `IRequestHandler<,>` in the same file
- `{Name}Validator.cs` — FluentValidation validator (optional)

Scaffold new use cases:
```bash
# From src\Application\
dotnet new ca-usecase --name CreateTodoItem --feature-name TodoItems --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

### MediatR pipeline behaviours (applied globally, in order)
`UnhandledExceptionBehaviour` → `AuthorizationBehaviour` → `ValidationBehaviour` → `PerformanceBehaviour` → `LoggingBehaviour`

### Authorization
Decorate MediatR request records with the custom `[Authorize]` attribute (from `Application.Common.Security`, **not** `Microsoft.AspNetCore.Authorization`). `AuthorizationBehaviour` enforces it via `IIdentityService`.

### Minimal API endpoint registration
Implement `IEndpointGroup` (in `Web.Infrastructure`). The class name determines the route prefix (`/api/{ClassName}`) and the OpenAPI tag. Override `static virtual string? RoutePrefix` for custom/nested paths. All implementations are auto-discovered from the assembly via `MapEndpoints()`.

### Domain entities
- Inherit `BaseEntity` (int PK, domain events) or `BaseAuditableEntity` (adds `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`).
- Raise domain events directly on the entity (e.g., `AddDomainEvent(new TodoItemCompletedEvent(this))`).
- Events are dispatched by `DispatchDomainEventsInterceptor` before `SaveChanges`.
- Audit fields are set automatically by `AuditableEntityInterceptor` — do not set them manually.

### EF Core configuration
Entity configurations go in `src\Infrastructure\Data\Configurations\`. `ApplicationDbContext.OnModelCreating` loads them automatically via `ApplyConfigurationsFromAssembly`.

### Central package management
All NuGet versions are managed in `Directory.Packages.props` at the solution root. Add `<PackageVersion>` entries there; reference packages in `.csproj` without a version attribute.

### Functional tests
Tests use `TestApp.SendAsync(command)` to dispatch MediatR requests directly against a real database (Respawn resets state between tests). Test classes inherit `TestBase` (which calls `TestApp.ResetState()` in `[SetUp]`). Use `Shouldly` for assertions.

### Naming conventions (enforced via `.editorconfig`)
- Private instance fields: `_camelCase`
- Private static fields: `s_camelCase`
- Avoid `var` — prefer explicit types
- File-scoped namespaces
- `TreatWarningsAsErrors = true` — all warnings are errors

### Implicit usings
Each project has a `GlobalUsings.cs` file. Common namespaces (MediatR, FluentValidation, AutoMapper, EF Core) are included globally — do not add redundant `using` statements.

### Service name constants
Use constants from `LinguaSpace.Shared.Services` (e.g., `Services.WebApi`, `Services.Database`) when referencing service names in `AppHost` or elsewhere.

# Project Instructions for AI Agent

You are an AI coding assistant working on an ASP.NET Core MVC application built with C# and .NET.

## Project Overview

This is a web application built with ASP.NET Core MVC following a layered architecture pattern. It uses Entity Framework Core for data access and follows Microsoft's recommended project structure.

## Technology Stack

- **Framework:** ASP.NET Core 10.0 (MVC)
- **Language:** C# 13 / .NET 10
- **ORM:** Entity Framework Core
- **Database:** SQL Server (LocalDB for development)
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views with Bootstrap 5, jQuery
- **Testing:** xUnit, Moq, FluentAssertions
- **Logging:** Serilog
- **API Documentation:** Swagger / OpenAPI (Swashbuckle)

## Project Structure

```
src/
├── MyApp.Web/              # ASP.NET Core MVC web project (entry point)
│   ├── Controllers/        # MVC controllers
│   ├── Views/              # Razor views (.cshtml)
│   ├── ViewModels/         # View-specific models
│   ├── wwwroot/            # Static files (CSS, JS, images)
│   ├── Areas/              # Feature-based areas (Admin, API, etc.)
│   └── Program.cs          # App entry point and DI configuration
├── MyApp.Core/             # Domain models, interfaces, business logic
│   ├── Entities/           # Domain entity classes
│   ├── Interfaces/         # Repository and service interfaces
│   ├── Services/           # Business logic / domain services
│   └── Enums/              # Shared enumerations
├── MyApp.Infrastructure/   # Data access, external integrations
│   ├── Data/               # DbContext, migrations, seed data
│   ├── Repositories/       # Repository implementations
│   └── Services/           # Infrastructure service implementations
tests/
├── MyApp.UnitTests/        # Unit tests (Core + Web)
└── MyApp.IntegrationTests/ # Integration tests (database, API)
```

## Coding Standards

### General
- Use **file-scoped namespaces** (`namespace MyApp.Core;`)
- Use **primary constructors** where appropriate
- Use **nullable reference types** — all projects have `<Nullable>enable</Nullable>`
- Use **implicit usings** — don't add `using System;` etc.
- Prefer **pattern matching** and **switch expressions** over if/else chains
- Use **records** for DTOs and value objects
- Use **`var`** when the type is obvious from the right-hand side

### Naming Conventions
- **PascalCase** for classes, methods, properties, and public fields
- **camelCase** for local variables and parameters
- **_camelCase** for private fields (with underscore prefix)
- **I** prefix for interfaces (e.g., `IUserRepository`)
- **Async** suffix for async methods (e.g., `GetUsersAsync`)
- Controllers: `[Name]Controller` (e.g., `ProductsController`)
- ViewModels: `[Name]ViewModel` (e.g., `ProductDetailViewModel`)

### Architecture Rules
- Controllers should be thin — delegate to services
- Never put business logic in controllers or repositories
- Use constructor injection for all dependencies
- Repository interfaces go in `Core`, implementations in `Infrastructure`
- Use `ILogger<T>` for logging, never `Console.WriteLine`
- All public API endpoints must have `[ProducesResponseType]` attributes
- Use `FluentValidation` or data annotations for input validation

### Entity Framework
- Use **code-first** migrations
- DbContext is registered as `scoped` in DI
- Use `AsNoTracking()` for read-only queries
- Never expose `IQueryable` outside the repository layer
- Include navigation properties explicitly with `.Include()`
- Use `ConfigureAwait(false)` in library projects (Core, Infrastructure)

### Error Handling
- Use `ProblemDetails` for API error responses
- Use custom exception types for domain errors (e.g., `NotFoundException`, `ValidationException`)
- Global exception handling middleware for unhandled exceptions
- Never swallow exceptions silently — always log them

### Testing
- Test naming: `MethodName_Scenario_ExpectedResult`
- Use **Arrange-Act-Assert** pattern
- Use `Moq` for mocking dependencies
- Use `FluentAssertions` for readable assertions
- Integration tests use `WebApplicationFactory<Program>`
- Each test class should have a corresponding source class

## Important Notes

- The `appsettings.json` should never contain production secrets — use User Secrets or environment variables
- Migrations are in `MyApp.Infrastructure/Data/Migrations/`
- The `Areas/API/` directory contains API controllers that return JSON (not views)
- Swagger UI is available at `/swagger` in development mode
- Health checks are configured at `/health` and `/health/ready`

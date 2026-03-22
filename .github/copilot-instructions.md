# GitHub Copilot Instructions

This file provides repository-specific guidance for GitHub Copilot agents working on the PTT Game Library project.

## Project Overview

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# 13 / .NET 10 |
| Frontend | Blazor Server (Hybrid Mode) with Fluent UI |
| Backend | ASP.NET Core Web API |
| Background Jobs | Azure Functions (Timer Triggers) |
| Database | Entity Framework Core (InMemory for dev, Azure SQL for prod) |
| Authentication | Azure AD B2C / Microsoft Identity |
| Testing | xUnit + Moq + FluentAssertions |

## Repository Structure

```
```

## Build Commands

**ALWAYS run these commands from the repository root (`C:\Source\GitHub\claw-mail-cal-cli`).**

### Restore Dependencies
```powershell
dotnet restore src/ClawMailCalCli/ClawMailCalCli.csproj
```

### Build
```powershell
dotnet build src/ClawMailCalCli/ClawMailCalCli.csproj --configuration Release --no-restore
```

### Run Tests
```powershell
dotnet test tests/ --configuration Release --no-build --verbosity normal
```

### Run the Application
```powershell
dotnet run --project src/ClawMailCalCli/ClawMailCalCli.csproj
```

### Format Code
```powershell
dotnet format src/
```

## Key Coding Standards

**Coding standards are enforced through path-specific instruction files in `.github/instructions/`.** Key points:

1. **Indentation**: Always use tabs (not spaces) for indentation - enforced by `.editorconfig`
2. **Use C# 13 features**: Primary constructors, simplified collections (`[]` not `Array.Empty<T>()`), record types, pattern matching
3. **Fluent UI for Blazor**: Always use `Microsoft.FluentUI.AspNetCore.Components` - never raw HTML elements
4. **One class per file**: Never multiple classes in one file
5. **Naming**: `PascalCase` for types/methods, `camelCase` for locals/parameters, `I` prefix for interfaces
6. **Async/await**: Use for all I/O operations
7. **Dependency injection**: Constructor injection only, no service locator
8. **Logging guards**: Always wrap log statements with `logger.IsEnabled()` checks in Azure Functions
9. **No regions**: Never use `#region`
10. **Explicit braces**: Always use block braces for control statements

## Testing Conventions

- Use **xUnit** for all tests
- Use **Moq** for mocking
- Use **FluentAssertions** for assertions
- Test naming: `MethodName_Scenario_ExpectedResult`
- Follow Arrange-Act-Assert pattern
- Target 80%+ coverage on business logic

## Secrets Management

- **NEVER** hardcode secrets in `appsettings.json` or source code
- Use **User Secrets** for local development
- Use **Azure Key Vault** for production
- Keep placeholder values in `appsettings.json` to document structure

## File Ignore Patterns

Do not modify or include in diffs:
- `**/bin/**`, `**/obj/**`
- `**/.vs/**`
- `**/node_modules/**`
- `**/*.user`
- `**/appsettings.*.json` (except `appsettings.json`)
- `**/local.settings.json`

## Validation Checklist

Before completing any task, verify:

1. ✅ Code compiles: `dotnet build`
2. ✅ Tests pass: `dotnet test`
3. ✅ Code is formatted: `dotnet format`
4. ✅ No secrets in code
5. ✅ Follows coding standards from `.github/instructions/`

## Agent Personas

See `docs/architecture/agents.md` for specialized agent behaviors:
- **Code Reviewer**: Security, performance, architecture focus
- **Test Generator**: xUnit/Moq patterns
- **Game Data Expert**: Xbox/Steam API integration
- **Blazor UI Expert**: Fluent UI components
- **Recommendation Engine**: Algorithm design
- **Deployment Engineer**: Azure deployment

## Additional Context

- **Architecture details**: `docs/architecture/architecture.md`
- **User stories**: `docs/user-stories/` - Individual story implementations
- **Project requirements**: `docs/architecture/requirements.md`

Trust these instructions. Only search the codebase if information here is incomplete or found to be incorrect.

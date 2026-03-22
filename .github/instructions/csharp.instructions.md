---
applyTo: "**/*.cs"
---

# C# Code Instructions

Follow these conventions for all C# files in this repository.

## Language Version
- Use C# 13 features: primary constructors, simplified collections, record types, pattern matching, global usings

## Project Context

- **Project Type**: Web API / Console App / Blazor App / Microservice
- **Language**: C#
- **Framework / Libraries**: .NET 10 / ASP.NET Core / Entity Framework Core / xUnit
- **Architecture**: Clean Architecture / MVC / Onion / CQRS

## Naming Conventions

- Always adhere to single responsibility and prioritize discoverability.
- Please put classes in separate files and folders based on their functionality.
- Use C#-idiomatic patterns and follow .NET coding conventions.
- `PascalCase` for classes, interfaces, enums, properties, methods
- `camelCase` for local variables, parameters, private fields
- Prefix interfaces with `I` (e.g., `IGameRepository`)
- Use singular names for classes and interfaces unless representing a collection.
- Descriptive names only - never abbreviate (e.g., `GoldPieces` not `Gp`)
- Never use common test acronyms like `sut` — name the variable after its type (e.g., `gameService`, `browserSpeechRecognitionService`)
- **Exception variables in catch blocks must never use abbreviations like `ex` or `e`** — use `exception` for `catch (Exception ...)`, and descriptive full names for specific exception types:

```csharp
// ✅ Correct — use `exception` for the base Exception type
catch (Exception exception)
{
    logger.LogError(exception, "Something failed");
}

// ✅ Correct — use a descriptive full name for specific exception types
catch (HttpRequestException httpRequestException)
{
    logger.LogError(httpRequestException, "HTTP request failed");
}

// ❌ Never use abbreviated exception variable names
catch (Exception ex)
{
    logger.LogError(ex, "Something failed");
}
```
- Name variables explicitly and descriptively so their purpose is clear from the name (e.g., `var myServiceClient = new MyServiceClient();`, `var releaseAuditResults = ...;`, `var userDisplayName = ...;`). Avoid generic names like `data`, `item`, or `value` unless context is extremely clear.
- Format using `dotnet format` or IDE auto-formatting tools.
- Prioritize readability, testability, and SOLID principles.

## Code Structure

- **Indentation**: Always use tabs (not spaces) for indentation. Tab width is set to 4 spaces for display.
- Organize code into feature-based folders (e.g., `Services`, `Providers`, `Clients`).
- **Place interfaces in `PttGameLibrary.Core/Interfaces/`** so they are shared across all projects. This is the canonical location for all interface abstractions in this solution.
  - Namespace: `PttGameLibrary.Core.Interfaces`
  - Exception: interfaces that have a hard dependency on a project-specific framework type (e.g., `HttpRequestData` from the Azure Functions SDK, or a Web-only enum) must stay in a local `Interfaces/` subfolder within that project (e.g., `Authentication/Interfaces/`, `Services/Interfaces/`). Their namespace must reflect the subfolder: `PttGameLibrary.Functions.Authentication.Interfaces`, `PttGameLibrary.Web.Services.Interfaces`, etc.
  - Consuming files (implementations, callers) must add the corresponding `using PttGameLibrary.Core.Interfaces;` statement.
- One public class per file - never leave multiple classes in one file; enforce one public type per file.
- **One record per file** - each record type must be in its own separate file.
- **Model and DTO records** - always place record types (especially DTOs and models) in dedicated `Models/` folders organized by purpose (e.g., `Models/Clients/XboxApi/`, `Models/Domain/`, `Models/DTOs/`).
- Refactor any composite file into separate files per class following repo guidelines.
- Remove scaffold files (Class1.cs) and organize by feature folders.
- Never use `#region`
- Always use block braces for control statements, even if single-line
- Avoid inline if-statements. Use explicit scoping for clarity and maintainability.
- Prefer expression-bodied members for simple properties/methods
- Use `[]` instead of `Array.Empty<T>()`

## Primary Constructors

**IMPORTANT**: Always keep constructor parameters on a single line, regardless of length. Always place inheritance/interfaces on a new line, indented with 1 tab.

```csharp
// ✅ Correct - single line parameters, inheritance on new line
public class MyService(ILogger<MyService> logger, IRepository repository)
    : IMyService
{
    // ...
}

// ✅ Correct - primary constructor with base class call
public class DiscoverPage(IPage page)
    : BasePage(page)
{
    // ...
}

// ✅ Correct - repository with dependency injection
public class GameRepository(ApplicationDbContext context)
    : IGameRepository
{
    // ...
}

// ✅ Correct - even very long constructors stay on one line
public class XboxApiClient(IXboxAuthApi authApi, IXboxUserAuthApi userAuthApi, IXboxXstsApi xstsApi, IXboxProfileApi profileApi, IXboxTitleHubApi titleHubApi, IXboxAchievementsApi achievementsApi, IXboxCollectionsApi collectionsApi, IXboxGamePassApi gamePassApi, IMicrosoftDisplayCatalogApi displayCatalogApi, IConfiguration configuration)
    : IXboxApiClient
{
    // ...
}

// ❌ NEVER DO THIS - multi-line constructor parameters
public class MyService(
    ILogger<MyService> logger,
    IRepository repository)
    : IMyService
{
    // ...
}

// ❌ NEVER DO THIS - inheritance on same line as constructor
public class GameRepository(ApplicationDbContext context) : IGameRepository
{
    // ...
}

// ❌ NEVER DO THIS - inheritance on same line with base call
public class DiscoverPage(IPage page) : BasePage(page)
{
    // ...
}
```

## Method Signatures

**IMPORTANT**: Always keep method signatures on a single line, regardless of length. This applies to both interface declarations and implementations.

### Rules:
1. Always use single-line signatures
2. Never split parameters across multiple lines
3. Apply to both interface declarations and implementations
4. Keep inheritance/interfaces on a new line (indented 1 tab) for class declarations

```csharp
// ✅ Correct - single line signature
Task<string?> GetExternalGameIdAsync(Guid userId, Guid queueItemId, CancellationToken cancellationToken = default);

// ✅ Correct - repository method single line
public async Task<IReadOnlyList<Game>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
{
    // ...
}

// ✅ Correct - controller action single line
public async Task<IActionResult> SyncAccount(Guid accountId, CancellationToken cancellationToken)
{
    // ...
}

// ✅ Correct - service method single line
Task<IReadOnlyList<WishlistItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

// ✅ Correct - even very long signatures stay on one line
Task<ComplexResult> ProcessComplexOperationAsync(Guid userId, string externalSystemId, ComplexRequest requestData, ProcessingOptions options, CancellationToken cancellationToken = default);

// ❌ NEVER DO THIS - multi-line method signatures
Task<string?> GetExternalGameIdAsync(
    Guid userId,
    Guid queueItemId,
    CancellationToken cancellationToken = default);

// ❌ NEVER DO THIS - splitting method signatures
public async Task<IReadOnlyList<Game>> GetByIdsAsync(
    IEnumerable<Guid> ids,
    CancellationToken cancellationToken = default)
{
    // ...
}

// ❌ NEVER DO THIS - repository method split across lines
public async Task<bool> ExistsAsync(
    Guid userId,
    Guid gameId,
    CancellationToken cancellationToken = default)
{
    // ...
}
```

**Return Types**: Prefer custom record types over tuples for method return values. Avoid tuples for any method that returns multiple values — this applies to both public and private methods:

```csharp
// ✅ Correct - custom record type
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
Task<TokenResponse?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);

// ✅ Correct - named record for internal/private helpers too
internal record GameTagInfo(string? Genre, IReadOnlyList<string> Tags);
private static GameTagInfo? ResolveFromQueue(string gameName, IReadOnlyList<RecommendationQueueItem> queue) { /* ... */ }

// ❌ Never use tuple return types
Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)?> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
private static (string? Genre, IReadOnlyList<string> Tags)? ResolveFromQueue(string gameName) { /* ... */ }
```

## Inheritance and Interfaces

**IMPORTANT**: Always place inherited classes and implemented interfaces on a new line, indented with 1 tab. Never place them on the same line as the class/constructor declaration.

```csharp
// ✅ Correct - inheritance on new line, indented
public class MyService
    : IMyService, IDisposable
{
    // ...
}

// ✅ Correct - with primary constructor
public class GameRepository(ApplicationDbContext context)
    : IGameRepository
{
    // ...
}

// ✅ Correct - multiple interfaces
public class UserService(IUserRepository repo, ILogger<UserService> logger)
    : IUserService, IDisposable
{
    // ...
}

// ❌ NEVER DO THIS - inheritance on same line
public class MyService : IMyService, IDisposable
{
    // ...
}

// ❌ NEVER DO THIS - with primary constructor on same line
public class GameRepository(ApplicationDbContext context) : IGameRepository
{
    // ...
}
```

## Function Calls

Keep function call arguments on a single line when reasonable:

```csharp
// ✅ Correct - single line
var result = await ExecuteAsync("operation", () => api.CallAsync(param), cancellationToken);

// ❌ Avoid - multi-line function arguments
var result = await ExecuteAsync(
    "operation",
    () => api.CallAsync(param),
    cancellationToken);
```

## Dependency Injection

- Use constructor injection for all dependencies.
- Register services with the correct lifetime (`AddSingleton`, `AddScoped`, `AddTransient`).
- Do not use service locator patterns.

## Async/Await

- Use `async`/`await` for all I/O-bound operations
- Suffix async methods with `Async`
- Pass `CancellationToken` where appropriate

## Unit Testability

- Depend on abstractions (interfaces), not concrete types.
- Avoid static classes for business logic.
- Use dependency injection for all external dependencies.
- Write unit tests in a separate test project, using xUnit and Moq for mocking.

## Nullable Reference Types
- Enable nullable reference types (`#nullable enable`)
- Use `var` when type is obvious, explicit types otherwise
- Use `readonly` for fields not reassigned after construction

## String Checks

**Always prefer `string.IsNullOrWhiteSpace` over `string.IsNullOrEmpty`** when testing whether a string has a meaningful value, unless you have a specific reason to treat whitespace-only strings as valid:

```csharp
// ✅ Correct - treats whitespace-only strings as empty/missing
if (!string.IsNullOrWhiteSpace(baseUrl))
{
    // use baseUrl
}

// ❌ Avoid - misses whitespace-only strings
if (!string.IsNullOrEmpty(baseUrl))
{
    // use baseUrl
}
```

## Testing

- Use xUnit for tests
- Use Moq for mocking
- Use FluentAssertions for assertions
- Test naming: `MethodName_Scenario_ExpectedResult`
- Follow Arrange-Act-Assert pattern
- Attempt to get at least 80% code coverage on business logic.
- Prefer TDD for critical business logic and application services.

---
applyTo: "**/tests/**/*.cs"
---

# Test Instructions

Follow these conventions for all test files in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## Framework
- **xUnit** for test framework
- **Moq** for mocking
- **FluentAssertions** for assertions

## Test Naming
Use the pattern: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task GetRecommendationsAsync_WithNoSeenGames_ReturnsAllGames()
```

## Test Structure
Follow Arrange-Act-Assert with clear section comments:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var mockRepo = new Mock<IGameRepository>();
    mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    var gameService = new GameService(mockRepo.Object);

    // Act
    var result = await gameService.GetGamesAsync();

    // Assert
    result.Should().BeEmpty();
}
```

## Class Structure

Use **primary constructors** when injecting dependencies. Place interfaces on a new line, indented one tab:

```csharp
public class GameRecommendationServiceTests(Mock<ApplicationDbContext> contextMock)
	: IDisposable
{
    private readonly Mock<ApplicationDbContext> _contextMock;
    private readonly GameRecommendationService _gameRecommendationService;

    public GameRecommendationServiceTests()
    {
        _contextMock = new Mock<ApplicationDbContext>();
        _gameRecommendationService = new GameRecommendationService(_contextMock.Object);
    }

	[Fact]
	public async Task Test2() { }
}
```

For fixtures injected via `IClassFixture<T>`, use a primary constructor and reference the fixture parameter directly (no backing field needed):

```csharp
public sealed class MyIntegrationTests(MyWebApplicationFactory factory)
	: IClassFixture<MyWebApplicationFactory>, IAsyncLifetime
{
	[Fact]
	public async Task Test() => await factory.DoSomethingAsync();
}
```

## Parameterized Tests
Use `[Theory]` with `[InlineData]`:

```csharp
[Theory]
[InlineData("Action", true)]
[InlineData("Horror", false)]
[InlineData("", false)]
public void IsValidGenre_WithInput_ReturnsExpected(string genre, bool expected)
{
    var result = GenreValidator.IsValid(genre);
    result.Should().Be(expected);
}
```

## Unit vs Integration Tests

All tests must be categorized using xUnit `[Trait]` at the class level:

```csharp
// Unit test — fast, uses only mocks, no real infrastructure
[Trait("Category", "Unit")]
public class GameServiceTests { }

// Integration test — uses real infrastructure (InMemory DB, WebApplicationFactory, Playwright, etc.)
[Trait("Category", "Integration")]
public class GameRepositoryTests : IDisposable { }
```

### Unit Tests
- Use `[Trait("Category", "Unit")]`
- Use mocks for all dependencies (Moq)
- No real databases, HTTP services, or browser automation
- Should complete in under 5 seconds per test
- Run on every CI build

### Integration Tests
- Use `[Trait("Category", "Integration")]`
- May use real infrastructure: EF Core InMemory DB, `WebApplicationFactory`, Playwright, `EphemeralDataProtectionProvider`, etc.
- May take more than 5 seconds to run (especially Playwright/E2E tests)
- **Not run during the CI build** — must be run manually or in a separate pipeline step
- File names should end in `IntegrationTests.cs` when possible for clarity

### CI Filter
The CI pipeline runs only unit tests:
```
dotnet test --filter "Category=Unit"
```

To run only integration tests locally:
```
dotnet test --filter "Category=Integration"
```

## Best Practices
- One assertion per test when possible
- Keep tests isolated and independent
- Use mocks for external dependencies
- Never use real external services in unit tests
- Target 80%+ coverage on business logic

## Variable Naming
- Use descriptive variable names, never use acronyms or abbreviations
- Name test subject variables after their type (e.g., `gameService`, `browserSpeechRecognitionService`) — never use `sut`
- For `CancellationTokenSource`, use `cancellationTokenSource` (not `cts`)

```csharp
// ✅ Correct
var gameService = new GameService(mockRepo.Object);
var browserSpeechRecognitionService = new BrowserSpeechRecognitionService(jsRuntime, logger);
var cancellationTokenSource = new CancellationTokenSource();
cancellationTokenSource.Cancel();
await act.Should().ThrowAsync<OperationCanceledException>();

// ❌ Avoid
var sut = new GameService(mockRepo.Object);
var cts = new CancellationTokenSource();
cts.Cancel();
```

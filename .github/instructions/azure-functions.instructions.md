---
applyTo: "**/PttGameLibrary.Functions/**/*.cs"
---

# Azure Functions Instructions

Follow these conventions for all Azure Functions in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## Logging Guards
**Always guard log statements** with `IsEnabled` checks to avoid string interpolation overhead:

```csharp
// ✅ Correct
if (logger.IsEnabled(LogLevel.Information))
{
    logger.LogInformation("Processing user {UserId}", userId);
}

// ❌ Avoid
logger.LogInformation("Processing user {UserId}", userId);
```

Apply this pattern to `LogInformation`, `LogWarning`, `LogError`, and `LogDebug`.

## Timer Trigger Format
Use CRON expressions with descriptive comments:

```csharp
[Function(nameof(SteamWishlistSyncFunction))]
public async Task Run(
    [TimerTrigger("0 0 */6 * * *")] TimerInfo timerInfo, // Every 6 hours
    CancellationToken cancellationToken)
{
}
```

## Current Function Schedules
| Function | CRON | Interval |
|----------|------|----------|
| SteamWishlistSync | `0 0 */6 * * *` | Every 6 hours |
| SteamUpcomingGamesSync | `0 0 */12 * * *` | Every 12 hours |
| XboxWishlistSync | `0 0 */6 * * *` | Every 6 hours |
| XboxGamePassSync | `0 0 */12 * * *` | Every 12 hours |
| RecommendationQueueBuilder | `0 0 * * * *` | Every hour |

## Error Handling
- Catch and log exceptions with full context
- Don't let exceptions bubble up unhandled
- Use structured logging with named parameters

## Dependency Injection
Functions use `Program.cs` for DI registration. Inject dependencies via primary constructor:

```csharp
public class MyFunction(ILogger<MyFunction> logger, IMyService service)
{
    [Function(nameof(MyFunction))]
    public async Task Run([TimerTrigger("...")] TimerInfo timer)
    {
        // Use logger and service
    }
}
```

## Method Signatures
Keep function method signatures on a single line when possible, following the same rules as C# instructions:

```csharp
// ✅ Correct - single line with HttpTrigger attribute
[Function("AccountSync")]
public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/account/{accountId}")] HttpRequestData req, string accountId, CancellationToken cancellationToken)
{
    // Function body
}

// ❌ Avoid - multi-line for reasonable length signatures
[Function("AccountSync")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/account/{accountId}")] HttpRequestData req,
    string accountId,
    CancellationToken cancellationToken)
{
    // Function body
}
```

Only use multi-line signatures when the line exceeds 120 characters.

---
applyTo: "**/*Client.cs,**/*Service.cs,**/Services/**/*.cs"
---

# HTTP Client Best Practices

Follow these guidelines for HTTP client usage in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## Prefer Refit over Raw HttpClient

**Always prefer Refit over raw HttpClient** for external API calls:

- Use Refit to define typed, declarative HTTP client interfaces
- Refit provides compile-time safety and cleaner code than manual HttpClient usage
- Define API contracts as interfaces with attributes (e.g., `[Get("/api/users")]`)

## Always Use Polly for HTTP Resilience

**Always use Polly for HTTP resilience**:

- Add retry policies for transient failures using `HttpPolicyExtensions.HandleTransientHttpError()`
- Use exponential backoff for retries (e.g., `WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))`)
- Add circuit breaker policies to prevent cascading failures

## Configuration Example

Register Refit clients with `AddRefitClient<IMyApi>()` and chain Polly policies with `.AddPolicyHandler()`:

```csharp
// Define Refit interface
public interface IMyApi
{
    [Get("/api/items/{id}")]
    Task<Item> GetItemAsync(int id, CancellationToken cancellationToken = default);
}

// Configure in DI
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

services.AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);
```

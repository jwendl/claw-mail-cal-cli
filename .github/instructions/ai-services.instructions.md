---
applyTo: "**/*Embedding*.cs,**/*Similarity*.cs,**/*AI*.cs,**/*Semantic*.cs"
---

# AI Services Instructions

Follow these guidelines for AI and machine learning services in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## Overview

The PTT Game Library uses Azure AI Foundry (Azure OpenAI + Semantic Kernel) for:

1. **Game recommendation re-ranking** — `IAiFoundryService.RerankGamesAsync` re-orders deterministically scored candidates using an LLM.
2. **Semantic embeddings** — `IAiFoundryService.GetEmbeddingsAsync` generates float-array vectors used as similarity tiebreakers in `GameSimilarityService`.
3. **Natural language filter parsing** — `IAiFoundryService.ParseNaturalLanguageFilterAsync` converts free-form voice filter phrases (e.g., "no sports besides skateboarding") into structured JSON.

All three operations go through the single `IAiFoundryService` interface (see `src/PttGameLibrary.Core/Interfaces/IAiFoundryService.cs`).

## Azure AI Foundry / Azure OpenAI Integration

### Authentication

**Always use `DefaultAzureCredential`** — never API keys in code:

```csharp
var credential = new DefaultAzureCredential();
kernelBuilder.AddAzureOpenAIChatCompletion(modelDeployment, endpoint, credential);
kernelBuilder.AddAzureOpenAIEmbeddingGenerator(embeddingDeployment, endpoint, credential);
```

### Semantic Kernel Initialization

Build the `Kernel` lazily and cache it. Use a `SemaphoreSlim` to prevent double-initialization under concurrency:

```csharp
private Kernel? _kernel;
private readonly SemaphoreSlim _kernelLock = new(1, 1);

private async Task<Kernel> GetOrCreateKernelAsync(CancellationToken cancellationToken)
{
	if (_kernel is not null)
	{
		return _kernel;
	}

	await _kernelLock.WaitAsync(cancellationToken);
	try
	{
		_kernel ??= BuildKernel();
		return _kernel;
	}
	finally
	{
		_kernelLock.Release();
	}
}
```

### Configuration

Read model deployment names and endpoint from `IConfiguration` — never hardcode:

```csharp
var endpoint = configuration["AzureAIFoundry:Endpoint"]
	?? throw new InvalidOperationException("AzureAIFoundry:Endpoint configuration is missing or empty.");
var modelDeployment = configuration["AzureAIFoundry:ModelDeploymentName"] ?? "gpt-4o";
var embeddingDeployment = configuration["AzureAIFoundry:EmbeddingDeploymentName"] ?? "text-embedding-ada-002";
```

Expected `appsettings.json` keys (values stored in User Secrets or Key Vault):

```json
{
  "AzureAIFoundry": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ModelDeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

## Embedding Generation and Storage

### Generating Embeddings

Call `IAiFoundryService.GetEmbeddingsAsync` with a batch of text strings. The service returns one `float[]` per input string, in the same order:

```csharp
var texts = new List<string> { targetGameName };
texts.AddRange(candidates.Select(c => $"{c.Title} {c.Genre} {string.Join(" ", c.Tags)}"));

var embeddings = await aiFoundryService.GetEmbeddingsAsync(texts, cancellationToken);

if (embeddings.Count != texts.Count)
{
	// Service returned an unexpected count — fall back to deterministic ordering.
	return scored;
}
```

### Text Format for Game Embeddings

Combine title, genre, and tags into a single string for richer semantic context:

```csharp
$"{game.Title} {game.Genre} {string.Join(" ", game.Tags)}"
```

### Cosine Similarity

Use the static `CosineSimilarity(float[] a, float[] b)` helper pattern for comparing embedding vectors:

```csharp
private static float CosineSimilarity(float[] a, float[] b)
{
	if (a.Length != b.Length || a.Length == 0) return 0f;

	var dot = 0f;
	var normA = 0f;
	var normB = 0f;

	for (var i = 0; i < a.Length; i++)
	{
		dot += a[i] * b[i];
		normA += a[i] * a[i];
		normB += b[i] * b[i];
	}

	if (normA == 0f || normB == 0f) return 0f;
	return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
}
```

## Similarity Scoring Conventions

The current deterministic scoring algorithm (used by `GameSimilarityService`) is:

| Match type | Points |
|------------|--------|
| Genre match | **+2** |
| Tag overlap (per tag) | **+1** |
| Embedding cosine similarity | Tiebreaker only |

```csharp
// Genre match: 2 points
if (string.Equals(item.Genre, targetGenre, StringComparison.OrdinalIgnoreCase))
{
	score += 2;
}

// Tag overlap: 1 point per shared tag
score += item.Tags.Count(tag => targetTagSet.Contains(tag));
```

**Do not change these weights** without updating this document and the related tests. Changes to scoring weights require a deliberate decision because they affect the recommendation quality for all users.

### Embedding Tiebreaker

Apply embeddings only when two or more candidates share the same deterministic score:

```csharp
if (aiFoundryService is not null && HasTies(scored))
{
	scored = await ApplyEmbeddingTiebreakerAsync(scored, targetGameName, cancellationToken);
}
```

Sort by deterministic score descending first, then by cosine similarity descending:

```csharp
.OrderByDescending(s => s.Score)
.ThenByDescending(s => s.Similarity)
```

## Caching Strategies for AI API Calls

### In-Memory Tag Cache

`GameSimilarityService` caches resolved `GameTagInfo` (genre + tags) by normalized game name to avoid repeated database lookups. The cache is bounded to prevent unbounded memory growth:

```csharp
private const int MaxCacheEntries = 100;
private readonly ConcurrentDictionary<string, GameTagInfo> _tagCache = new(StringComparer.OrdinalIgnoreCase);

if (_tagCache.Count < MaxCacheEntries)
{
	_tagCache.TryAdd(cacheKey, resolved);
}
```

### Semantic Kernel Caching

The `Kernel` instance is created once per scoped `AiFoundryService` instance (per-request / per-Blazor-circuit) and cached for that service instance. This is the primary performance optimization for chat completion and embedding calls.

### Embedding Caching

For high-traffic scenarios, cache embedding vectors keyed by a hash of the input text using `IMemoryCache`. This avoids duplicate embedding API calls for the same game title across multiple users or requests:

```csharp
// ✅ Pattern: cache embeddings by normalized text hash
var cacheKey = $"embedding:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText)))}";
if (!memoryCache.TryGetValue(cacheKey, out float[]? cachedEmbedding))
{
	var embeddings = await aiFoundryService.GetEmbeddingsAsync([normalizedText], cancellationToken);
	cachedEmbedding = embeddings.Count > 0 ? embeddings[0] : [];
	memoryCache.Set(cacheKey, cachedEmbedding, TimeSpan.FromHours(24));
}
```

**Never cache embeddings on the raw text string** — use a hash to avoid holding large strings as memory keys.

## Fallback Patterns When AI Services Are Unavailable

### Required Fallbacks

Every AI call **must** have a deterministic fallback. Never let an AI failure propagate to the user as an error:

```csharp
catch (Exception exception)
{
	if (logger.IsEnabled(LogLevel.Warning))
	{
		logger.LogWarning(exception, "AI re-ranking failed, falling back to deterministic order");
	}

	// Fall back to deterministic order
	return candidates
		.OrderByDescending(c => c.DeterministicScore)
		.Select(c => c.GameId)
		.ToList();
}
```

### Optional AI Dependencies

When AI improves but does not gate a feature, declare `IAiFoundryService` as an optional dependency (nullable):

```csharp
public class GameSimilarityService(ApplicationDbContext context, ILogger<GameSimilarityService> logger, IAiFoundryService? aiFoundryService = null)
```

This allows the service to function without AI and simplifies unit testing by removing the need to always mock `IAiFoundryService`.

### Embedding Fallback

Return an empty list when embedding generation fails:

```csharp
catch (Exception exception)
{
	if (logger.IsEnabled(LogLevel.Warning))
	{
		logger.LogWarning(exception, "Embedding generation failed, returning empty embeddings");
	}
	return [];
}
```

### NLP Filter Fallback

Return `string.Empty` when natural language parsing fails, so the caller treats it as an unrecognized command:

```csharp
catch (Exception exception)
{
	if (logger.IsEnabled(LogLevel.Warning))
	{
		logger.LogWarning(exception, "NLP filter parsing failed for input '{Input}'", voiceInput);
	}
	return string.Empty;
}
```

## Cost Awareness

### Log Token Usage

When making chat completion calls, log the model and relevant context size at `Debug` level to support cost monitoring:

```csharp
if (logger.IsEnabled(LogLevel.Debug))
{
	logger.LogDebug("AI re-ranking {Count} candidates for profile with {GenreCount} genre preferences", candidates.Count, profile.FavoriteGenres.Count);
}
```

### Prefer Cached Embeddings

Always check the cache before calling `GetEmbeddingsAsync`. Embedding API calls incur per-token costs — redundant calls for the same game title across users are waste.

### Minimize Prompt Size

Build prompts programmatically to include only the data needed for the task. Avoid sending full game descriptions when titles, genres, and tags suffice:

```csharp
// ✅ Efficient — only sends the fields the model needs
$"- ID:{c.GameId} Title:{c.Title} Genre:{c.Genre ?? "Unknown"} Tags:{string.Join(",", c.Tags)} Score:{c.DeterministicScore}"

// ❌ Wasteful — includes unnecessary prose
$"The game '{c.Title}' is a {c.Genre} game described as: {c.Description}"
```

### Only Call AI When Needed

In `VoiceIntentProcessor`, the AI filter parser is only invoked when the input contains filter-like keywords. This prevents unnecessary AI calls for clearly unrelated voice inputs:

```csharp
if (!ContainsFilterKeywords(trimmed))
{
	return new VoiceIntent(VoiceIntentType.Unknown);
}

// Only now call the AI
var parsedFilter = await aiFoundryService.ParseNaturalLanguageFilterAsync(trimmed, cancellationToken);
```

Follow this pattern when adding new AI call sites — always apply a cheap pre-filter before making an API call.

## Prompt Engineering Conventions

### System Message Style

System messages should be concise and directive:

```csharp
chatHistory.AddSystemMessage(
	"You are a game recommendation assistant. Re-rank the provided game list based on user preferences. " +
	"Return only a comma-separated list of game IDs in order from most to least recommended, with no other text.");
```

### Structured Output

When the LLM must return structured data (IDs, JSON), instruct it to return only the data with no surrounding prose. Always validate and parse defensively:

```csharp
private static IReadOnlyList<Guid> ParseRerankResponse(string responseText, IReadOnlyList<RecommendationCandidate> candidates)
{
	var validIds = candidates.Select(c => c.GameId).ToHashSet();
	// ... parse and validate; append any candidates not mentioned in the AI response
}
```

## Testing Patterns for AI Services

### Unit Testing with Mocked IAiFoundryService

```csharp
[Trait("Category", "Unit")]
public class GameSimilarityServiceTests
{
	[Fact]
	public async Task GetSimilarGamesAsync_WithEmbeddingTiebreaker_OrdersByCosineSimilarity()
	{
		// Arrange
		var mockAiFoundryService = new Mock<IAiFoundryService>();
		mockAiFoundryService
			.Setup(s => s.GetEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync([[1f, 0f], [0.9f, 0.1f], [0.1f, 0.9f]]);

		// Note: pass aiFoundryService as optional parameter
		var gameSimilarityService = new GameSimilarityService(context, Mock.Of<ILogger<GameSimilarityService>>(), mockAiFoundryService.Object);

		// Act
		var results = await gameSimilarityService.GetSimilarGamesAsync("Half-Life 2", queue);

		// Assert
		results.Should().NotBeEmpty();
	}
}
```

### Testing Without AI (Optional Dependency)

When `IAiFoundryService` is optional, test the no-AI path by not passing it:

```csharp
var gameSimilarityService = new GameSimilarityService(context, Mock.Of<ILogger<GameSimilarityService>>());
// AI code paths are skipped; only deterministic scoring runs.
```

### Testing AiFoundryService Fallback Behavior

Verify that each method returns a safe fallback when the AI service throws:

```csharp
[Fact]
public async Task RerankGamesAsync_WhenAiThrows_ReturnsDeterministicOrder()
{
	// Arrange
	var mockKernel = /* ... setup Kernel mock to throw */;
	// ...

	// Act
	var results = await aiFoundryService.RerankGamesAsync(candidates, profile);

	// Assert — results must still be in deterministic score order
	results.Should().BeInDescendingOrder(id => candidates.First(c => c.GameId == id).DeterministicScore);
}
```

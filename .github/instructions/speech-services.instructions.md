---
applyTo: "**/*Speech*.cs,**/*Voice*.cs,**/wwwroot/js/speech*.js"
---

# Speech Service Instructions

Follow these guidelines for speech and voice services in this repository.

## Indentation

- **Always use tabs (not spaces) for indentation.** Tab width is set to 4 spaces for display.

## Overview

The PTT Game Library uses two speech subsystems:

1. **Browser Speech Recognition** — Web Speech API via JS interop (`BrowserSpeechRecognitionService`, `speechRecognition.js`)
2. **Text-to-Speech** — Browser `SpeechSynthesis` API by default (`BrowserTextToSpeechService`, `speechSynthesis.js`), with Azure Cognitive Services Neural TTS available for server-side synthesis (`AzureTextToSpeechService`)

## Browser Speech Recognition API Integration

### JS Interop Pattern

All browser speech API access goes through a JavaScript ES module loaded lazily on first use:

```csharp
private async Task<IJSObjectReference> GetModuleAsync()
{
	_jsObjectReference ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/speechRecognition.js");
	return _jsObjectReference;
}
```

- Load the module lazily and cache the reference as a private field.
- Always call the module reference, never `jsRuntime` directly, for JS operations.
- Dispose the `IJSObjectReference` in `DisposeAsync` by calling `"dispose"` first, then `DisposeAsync()`.

### DotNetObjectReference for Callbacks

The JS module calls back into C# using `DotNetObjectReference<T>`:

```csharp
var dotNetRef = DotNetObjectReference.Create(this);
var started = await module.InvokeAsync<bool>("startRecognition", cancellationToken, dotNetRef, "en-US");
if (started)
{
	_dotnetObjectReference = dotNetRef;
}
else
{
	dotNetRef.Dispose();
}
```

- Store the `DotNetObjectReference` as a field so it isn't garbage-collected while JS holds a reference.
- Dispose the `DotNetObjectReference` in `DisposeAsync`.
- Mark callback methods with `[JSInvokable]`.

### Callback Methods

JS-invokable methods must be `public` and match the signature the JS module expects:

```csharp
[JSInvokable]
public void OnSpeechResult(string text, bool isFinal)
{
	// Update transcript state; fire partial-result callback for interim results.
}

[JSInvokable]
public void OnSpeechError(string error)
{
	// Log the error and reset listening state.
	if (logger.IsEnabled(LogLevel.Warning))
	{
		logger.LogWarning("Speech recognition error: {Error}", error);
	}
}
```

### Concurrency

- Use `SemaphoreSlim _lock = new(1, 1)` to serialize `StartRecognitionAsync` and `StopRecognitionAsync`.
- Use `Lock _callbackLock = new()` (C# 13) to guard the mutable callback and transcript fields from concurrent JS-callback threads.
- Always `await _lock.WaitAsync(cancellationToken)` in a `try/finally` with `_lock.Release()`.

## VoiceIntent Model Structure

`VoiceIntent` is a record in `PttGameLibrary.Core.Models`:

```csharp
public record VoiceIntent(
	VoiceIntentType Type,
	string? GameName = null,      // SimilarityQuery
	string? RawFilter = null,     // FilterCommand
	SwipeDirection? SwipeDirection = null, // SwipeAction
	SortMode? SortMode = null,    // SortCommand
	string? SearchQuery = null);  // SearchQuery
```

- Only populate the property relevant to the `VoiceIntentType` — all others remain `null`.
- Return `new VoiceIntent(VoiceIntentType.Unknown)` for unrecognized input.

## VoiceIntentType Enum

| Value | Trigger | Populated Property |
|-------|---------|-------------------|
| `SimilarityQuery` | "show me games like …", "more like …" | `GameName` |
| `FilterCommand` | Natural language filter expressions | `RawFilter` |
| `SwipeAction` | "like", "pass", "next game", "details" | `SwipeDirection` |
| `HelpRequest` | "help", "what can I say?" | — |
| `SortCommand` | "sort by latest", "sort by recommended" | `SortMode` |
| `SearchQuery` | "search for …", "find …", "look up …" | `SearchQuery` |
| `Unknown` | Unrecognized input | — |

## How to Add a New Voice Intent Type

### Step 1 — Add the enum value

Add a new value to `VoiceIntentType` in `src/PttGameLibrary.Core/Enums/VoiceIntentType.cs` with an XML doc comment.

### Step 2 — Add a property to VoiceIntent (if needed)

If the new intent carries a payload, add a nullable property to the `VoiceIntent` record in `src/PttGameLibrary.Core/Models/VoiceIntent.cs`.

### Step 3 — Add a regex pattern in VoiceIntentProcessor

Add a `[GeneratedRegex(...)]` partial method in `src/PttGameLibrary.Infrastructure/Services/VoiceIntentProcessor.cs`:

```csharp
[GeneratedRegex(@"^(?:open|launch)\s+(.+?)(?:[.?!,]*)$", RegexOptions.IgnoreCase)]
private static partial Regex LaunchGamePattern();
```

Then check it in `ClassifyIntentAsync` in priority order (check simple patterns before AI fallback):

```csharp
var launchMatch = LaunchGamePattern().Match(trimmed);
if (launchMatch.Success)
{
	var gameName = launchMatch.Groups[1].Value.Trim().TrimEnd('.', '?', '!', ',');
	return new VoiceIntent(VoiceIntentType.LaunchGame, GameName: gameName);
}
```

### Step 4 — Handle the intent in the UI

In `src/PttGameLibrary.Web/Components/Pages/Discover.razor`, dispatch to the new handler in the voice intent switch or if-chain:

```csharp
VoiceIntentType.LaunchGame => await HandleLaunchGameAsync(intent.GameName!),
```

### Step 5 — Write a unit test

Add a test for the new regex pattern in `tests/PttGameLibrary.Infrastructure.Tests/Services/VoiceIntentProcessorTests.cs`:

```csharp
[Theory]
[InlineData("open Halo", "Halo")]
[InlineData("launch Minecraft", "Minecraft")]
public async Task ClassifyIntentAsync_LaunchPattern_ReturnsLaunchGameIntent(string input, string expectedGame)
{
	// Arrange
	var voiceIntentProcessor = new VoiceIntentProcessor(Mock.Of<ILogger<VoiceIntentProcessor>>(), Mock.Of<IAiFoundryService>());

	// Act
	var result = await voiceIntentProcessor.ClassifyIntentAsync(input);

	// Assert
	result.Type.Should().Be(VoiceIntentType.LaunchGame);
	result.GameName.Should().Be(expectedGame);
}
```

## Azure TTS Service Patterns

### SSML Building

Use `System.Security.SecurityElement.Escape` to HTML-encode dynamic content inserted into SSML:

```csharp
internal string BuildSsml(string voiceName, string message)
{
	var volumePercent = (int)(Volume * 100);
	var rateChangePercent = (int)Math.Round((Rate - 1.0) * 100);
	var rateStr = rateChangePercent >= 0 ? $"+{rateChangePercent}%" : $"{rateChangePercent}%";
	var language = _configuration["AzureSpeech:Language"] ?? "en-US";

	return $"""
		<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="{SecurityElement.Escape(language)}">
			<voice name="{SecurityElement.Escape(voiceName)}">
				<prosody rate="{rateStr}" volume="{volumePercent}%">
					{SecurityElement.Escape(message)}
				</prosody>
			</voice>
		</speak>
		""";
}
```

### Message Queuing

Azure TTS messages are queued via `System.Threading.Channels.Channel<string>` with `BoundedChannelFullMode.DropOldest` to avoid unbounded growth:

```csharp
_messageChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(20)
{
	FullMode = BoundedChannelFullMode.DropOldest,
	SingleReader = true,
});
```

- Use `TryWrite` for fire-and-forget enqueue (non-blocking).
- Process messages in a background loop started in the constructor.
- Cancel the background loop on `DisposeAsync` using a `CancellationTokenSource`.

### Voice Selection

Configure the voice name via `appsettings.json`. Only non-secret values belong in this file — store `Key` and `Region` in **User Secrets** for local development or **Azure Key Vault** for production:

```json
{
  "AzureSpeech": {
    "TtsVoiceName": "en-US-JennyNeural",
    "Language": "en-US"
  }
}
```

Supply the secret values via User Secrets (local) or Key Vault (production) — never commit them to source control:

```json
{
  "AzureSpeech": {
    "Key": "<set-in-user-secrets-or-key-vault>",
    "Region": "<set-in-user-secrets-or-key-vault>"
  }
}
```

- Default voice is `en-US-JennyNeural`.
- Never hardcode the voice name, key, or region — always read from configuration.

## Error Handling for Speech Services

### Microphone Permission

Call `RequestMicrophonePermissionAsync()` before starting recognition for the first time. The browser will prompt the user if permission has not been granted.

### Browser Support

Check `isSupported()` in the JS module before initializing the speech API. The C# service handles this by checking the return value of `startRecognition`:

```csharp
var started = await module.InvokeAsync<bool>("startRecognition", cancellationToken, dotNetRef, "en-US");
if (!started)
{
	if (logger.IsEnabled(LogLevel.Warning))
	{
		logger.LogWarning("Speech recognition could not be started (browser may not support the Web Speech API)");
	}
}
```

### JS Interop Unavailability (Pre-render)

Catch `JSException` and `InvalidOperationException` when JS interop is unavailable during Blazor pre-render:

```csharp
catch (Exception exception) when (exception is JSException or InvalidOperationException)
{
	if (logger.IsEnabled(LogLevel.Debug))
	{
		logger.LogDebug(exception, "JS interop not available");
	}
}
```

### Network Failures (Azure TTS)

Catch all exceptions in `SynthesizeSpeechAsync` but exclude disposal cancellation:

```csharp
catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
{
	if (logger.IsEnabled(LogLevel.Error))
	{
		logger.LogError(exception, "TTS synthesis error for message: {Message}", message);
	}
}
```

### Disabled State

Both recognition and TTS services have an `IsEnabled` flag. Always check it before performing any work:

```csharp
if (!IsEnabled || string.IsNullOrWhiteSpace(message) || _disposed)
{
	return Task.CompletedTask;
}
```

## Testing Patterns for Voice Intent Classification

### Unit Test Pattern

Mock `IAiFoundryService` and create `VoiceIntentProcessor` directly. Use `[Theory]` + `[InlineData]` to cover multiple phrases per intent:

```csharp
[Trait("Category", "Unit")]
public class VoiceIntentProcessorTests
{
	[Theory]
	[InlineData("like", VoiceIntentType.SwipeAction)]
	[InlineData("pass", VoiceIntentType.SwipeAction)]
	[InlineData("next game", VoiceIntentType.SwipeAction)]
	[InlineData("show details", VoiceIntentType.SwipeAction)]
	public async Task ClassifyIntentAsync_SwipePatterns_ReturnsSwipeAction(string input, VoiceIntentType expectedType)
	{
		// Arrange
		var mockAiFoundryService = new Mock<IAiFoundryService>();
		var voiceIntentProcessor = new VoiceIntentProcessor(Mock.Of<ILogger<VoiceIntentProcessor>>(), mockAiFoundryService.Object);

		// Act
		var result = await voiceIntentProcessor.ClassifyIntentAsync(input);

		// Assert
		result.Type.Should().Be(expectedType);
	}

	[Theory]
	[InlineData("show me games like Halo", "Halo")]
	[InlineData("more like Minecraft", "Minecraft")]
	[InlineData("similar to Elden Ring", "Elden Ring")]
	public async Task ClassifyIntentAsync_SimilarityPatterns_ReturnsGameName(string input, string expectedGame)
	{
		// Arrange
		var mockAiFoundryService = new Mock<IAiFoundryService>();
		var voiceIntentProcessor = new VoiceIntentProcessor(Mock.Of<ILogger<VoiceIntentProcessor>>(), mockAiFoundryService.Object);

		// Act
		var result = await voiceIntentProcessor.ClassifyIntentAsync(input);

		// Assert
		result.Type.Should().Be(VoiceIntentType.SimilarityQuery);
		result.GameName.Should().Be(expectedGame);
	}
}
```

### Testing BrowserSpeechRecognitionService

Mock `IJSRuntime` and `IJSObjectReference` to simulate browser API behavior without a real browser:

```csharp
var mockJsRuntime = new Mock<IJSRuntime>();
var mockModule = new Mock<IJSObjectReference>();
mockJsRuntime
	.Setup(js => js.InvokeAsync<IJSObjectReference>("import", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
	.ReturnsAsync(mockModule.Object);
mockModule
	.Setup(m => m.InvokeAsync<bool>("startRecognition", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
	.ReturnsAsync(true);

var browserSpeechRecognitionService = new BrowserSpeechRecognitionService(
	mockJsRuntime.Object,
	Mock.Of<IGameTelemetryService>(),
	Mock.Of<ILogger<BrowserSpeechRecognitionService>>());
```

## JavaScript Module Conventions

Speech-related JS files in `wwwroot/js/` must:

- Use ES module syntax (`export function …`) — no global state on `window`.
- Export a `dispose()` function that cleans up all held resources.
- Export an `isSupported()` function that tests browser capability before any API access.
- Never throw unhandled exceptions — catch errors and either return `false` or invoke the .NET error callback.

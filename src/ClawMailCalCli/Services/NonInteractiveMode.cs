namespace ClawMailCalCli.Services;

/// <summary>
/// Holds global CLI flags that affect service-layer behavior.
/// Registered as a singleton in DI and populated from the raw argument list
/// before Spectre.Console processes it.
/// </summary>
public sealed class NonInteractiveMode
{
	/// <summary>
	/// When <see langword="true"/>, the CLI must not block on interactive prompts.
	/// Any operation that would trigger a device-code flow should instead fail fast
	/// with an actionable error message.
	/// </summary>
	public bool IsNonInteractive { get; init; }

	/// <summary>
	/// When <see langword="true"/>, error output is serialized as JSON to stdout
	/// instead of being written as human-readable text via AnsiConsole.
	/// </summary>
	public bool IsJson { get; init; }
}

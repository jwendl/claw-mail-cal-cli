namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Base settings class that exposes the <c>--json</c> flag shared by all commands
/// that support machine-readable output.
/// </summary>
internal abstract class JsonOutputSettings
	: CommandSettings
{
	/// <summary>
	/// When <see langword="true"/>, outputs raw JSON to stdout instead of formatted console output.
	/// </summary>
	[CommandOption("--json")]
	public bool Json { get; init; }
}

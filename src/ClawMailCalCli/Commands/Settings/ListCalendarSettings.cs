namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>calendar list</c> command.
/// </summary>
internal sealed class ListCalendarSettings
	: CommandSettings
{
	/// <summary>
	/// When <see langword="true"/>, outputs raw JSON to stdout instead of a formatted table.
	/// </summary>
	[CommandOption("--json")]
	public bool Json { get; init; }
}

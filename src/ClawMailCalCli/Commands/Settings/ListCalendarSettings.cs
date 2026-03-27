namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>calendar list</c> command.
/// </summary>
internal sealed class ListCalendarSettings
	: CommandSettings
{
	/// <summary>
	/// The account to authenticate with. If omitted, the default account is used.
	/// </summary>
	[CommandOption("--account|-a")]
	public string? AccountName { get; init; }

	/// <summary>
	/// When <see langword="true"/>, outputs raw JSON to stdout instead of a formatted table.
	/// </summary>
	[CommandOption("--json")]
	public bool Json { get; init; }
}

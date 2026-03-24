namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>calendar read</c> command.
/// </summary>
internal sealed class ReadCalendarSettings
	: CommandSettings
{
	/// <summary>
	/// The event title (partial match) or unique Graph event ID to look up.
	/// </summary>
	[CommandArgument(0, "<query>")]
	public required string Query { get; init; }

	/// <summary>
	/// The account to authenticate with. If omitted, the default account from configuration is used.
	/// </summary>
	[CommandOption("--account|-a")]
	public string? AccountName { get; init; }

	/// <summary>
	/// When <see langword="true"/>, outputs raw JSON to stdout instead of formatted text.
	/// </summary>
	[CommandOption("--json")]
	public bool Json { get; init; }
}

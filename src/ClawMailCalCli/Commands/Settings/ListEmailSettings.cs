namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>email list</c> command.
/// </summary>
internal sealed class ListEmailSettings
	: CommandSettings
{
	/// <summary>
	/// The optional folder name to list messages from. Defaults to the inbox when omitted.
	/// </summary>
	[CommandArgument(0, "[folder-name]")]
	public string? FolderName { get; set; }

	/// <summary>
	/// When <see langword="true"/>, outputs raw JSON to stdout instead of a formatted table.
	/// </summary>
	[CommandOption("--json")]
	public bool Json { get; init; }
}

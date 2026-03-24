namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>email read</c> command.
/// </summary>
internal sealed class ReadEmailSettings : CommandSettings
{
	/// <summary>Gets or sets the account name to query.</summary>
	[CommandArgument(0, "<account-name>")]
	public required string AccountName { get; init; }

	/// <summary>Gets or sets the subject search text or exact Graph message ID.</summary>
	[CommandArgument(1, "<subject-or-id>")]
	public required string SubjectOrId { get; init; }

	/// <summary>
	/// When <see langword="true"/>, outputs raw JSON to stdout instead of formatted text.
	/// </summary>
	[CommandOption("--json")]
	public bool Json { get; init; }
}

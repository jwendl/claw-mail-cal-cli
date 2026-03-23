namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <see cref="Email.ReadEmailCommand"/>.
/// </summary>
internal sealed class ReadEmailSettings : CommandSettings
{
	/// <summary>Gets or sets the account name to query.</summary>
	[CommandArgument(0, "<account-name>")]
	public string AccountName { get; set; } = string.Empty;

	/// <summary>Gets or sets the subject search text or exact Graph message ID.</summary>
	[CommandArgument(1, "<subject-or-id>")]
	public string SubjectOrId { get; set; } = string.Empty;
}

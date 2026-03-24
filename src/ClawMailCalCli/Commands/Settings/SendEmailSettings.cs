namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>email send</c> command.
/// </summary>
internal sealed class SendEmailSettings
	: CommandSettings
{
	/// <summary>
	/// The recipient email address.
	/// </summary>
	[CommandArgument(0, "<to>")]
	public required string To { get; init; }

	/// <summary>
	/// The email subject.
	/// </summary>
	[CommandArgument(1, "<subject>")]
	public required string Subject { get; init; }

	/// <summary>
	/// The plain-text body content.
	/// </summary>
	[CommandArgument(2, "<content>")]
	public required string Content { get; init; }
}

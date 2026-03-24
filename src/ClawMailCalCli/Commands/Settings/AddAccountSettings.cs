namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>account add</c> command.
/// </summary>
internal sealed class AddAccountSettings
	: CommandSettings
{
	/// <summary>
	/// The name of the account to add.
	/// </summary>
	[CommandArgument(0, "<name>")]
	public required string Name { get; init; }

	/// <summary>
	/// The email address for the account.
	/// </summary>
	[CommandArgument(1, "<email>")]
	public required string Email { get; init; }
}

namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>account delete</c> command.
/// </summary>
internal sealed class DeleteAccountSettings
	: CommandSettings
{
	/// <summary>
	/// The name of the account to delete.
	/// </summary>
	[CommandArgument(0, "<name>")]
	public required string Name { get; init; }
}

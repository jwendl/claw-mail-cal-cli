namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>account set</c> command.
/// </summary>
internal sealed class SetAccountSettings
	: CommandSettings
{
	/// <summary>
	/// The name of the account to set as default.
	/// </summary>
	[CommandArgument(0, "<name>")]
	public required string Name { get; init; }
}

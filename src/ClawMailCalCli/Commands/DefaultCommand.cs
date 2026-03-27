using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands;

/// <summary>
/// Placeholder root command for the claw-mail-cal-cli application.
/// </summary>
internal sealed class DefaultCommand(IOutputService outputService)
	: Command<DefaultCommand.Settings>
{
	/// <summary>
	/// Settings for the <see cref="DefaultCommand"/>.
	/// </summary>
	internal sealed class Settings
		: CommandSettings
	{
	}

	/// <inheritdoc />
	public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		outputService.WriteMarkup("[green]claw-mail-cal-cli[/] is running.");
		return 0;
	}
}

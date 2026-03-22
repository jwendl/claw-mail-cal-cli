using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Sets the default account.
/// </summary>
internal sealed class SetAccountCommand(IAccountService accountService)
	: AsyncCommand<SetAccountCommand.Settings>
{
	/// <summary>
	/// Settings for the <see cref="SetAccountCommand"/>.
	/// </summary>
	internal sealed class Settings : CommandSettings
	{
		/// <summary>
		/// The name of the account to set as default.
		/// </summary>
		[CommandArgument(0, "<name>")]
		public required string Name { get; init; }
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		var set = await accountService.SetDefaultAccountAsync(settings.Name, cancellationToken);
		if (!set)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{settings.Name}[/]' does not exist.");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Default account set to '[yellow]{settings.Name}[/]'.");
		return 0;
	}
}

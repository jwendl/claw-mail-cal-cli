using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Sets the default account.
/// </summary>
internal sealed class SetAccountCommand(IAccountService accountService)
	: AsyncCommand<SetAccountSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, SetAccountSettings settings, CancellationToken cancellationToken)
	{
		var set = await accountService.SetDefaultAccountAsync(settings.Name, cancellationToken);
		if (!set)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' does not exist.");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Default account set to '[yellow]{Markup.Escape(settings.Name)}[/]'.");
		return 0;
	}
}

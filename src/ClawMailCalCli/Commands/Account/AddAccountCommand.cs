using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Adds a new account to the store.
/// </summary>
internal sealed class AddAccountCommand(IAccountService accountService)
	: AsyncCommand<AddAccountSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, AddAccountSettings settings, CancellationToken cancellationToken)
	{
		var added = await accountService.AddAccountAsync(settings.Name, settings.Email, cancellationToken: cancellationToken);
		if (!added)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' already exists.");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' added successfully.");
		return 0;
	}
}

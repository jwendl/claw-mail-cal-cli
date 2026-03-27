using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Adds a new account to the store.
/// </summary>
internal sealed class AddAccountCommand(IAccountService accountService, IOutputService outputService)
	: AsyncCommand<AddAccountSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, AddAccountSettings settings, CancellationToken cancellationToken)
	{
		var added = await accountService.AddAccountAsync(settings.Name, settings.Email, settings.Type, cancellationToken);
		if (!added)
		{
			var errorMessage = $"Account '{settings.Name}' already exists.";
			if (settings.Json)
			{
				outputService.WriteJsonError(errorMessage);
			}
			else
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' already exists.");
			}

			return 1;
		}

		var successMessage = $"Account '{settings.Name}' added successfully.";
		if (settings.Json)
		{
			outputService.WriteJson(new CommandResult(true, successMessage));
		}
		else
		{
			AnsiConsole.MarkupLine($"[green]✓[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' added successfully.");
		}

		return 0;
	}
}

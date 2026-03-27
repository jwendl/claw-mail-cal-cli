using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Deletes an account from the store.
/// </summary>
internal sealed class DeleteAccountCommand(IAccountService accountService, IOutputService outputService)
	: AsyncCommand<DeleteAccountSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, DeleteAccountSettings settings, CancellationToken cancellationToken)
	{
		var deleted = await accountService.DeleteAccountAsync(settings.Name, cancellationToken);
		if (!deleted)
		{
			var errorMessage = $"Account '{settings.Name}' does not exist.";
			if (settings.Json)
			{
				outputService.WriteJsonError(errorMessage);
			}
			else
			{
				AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' does not exist.");
			}

			return 1;
		}

		var successMessage = $"Account '{settings.Name}' deleted successfully.";
		if (settings.Json)
		{
			outputService.WriteJson(new CommandResult(true, successMessage));
		}
		else
		{
			AnsiConsole.MarkupLine($"[green]✓[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' deleted successfully.");
		}

		return 0;
	}
}

using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Sets the default account.
/// </summary>
internal sealed class SetAccountCommand(IAccountService accountService, IOutputService outputService)
	: AsyncCommand<SetAccountSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, SetAccountSettings settings, CancellationToken cancellationToken)
	{
		var set = await accountService.SetDefaultAccountAsync(settings.Name, cancellationToken);
		if (!set)
		{
			var errorMessage = $"Account '{settings.Name}' does not exist.";
			if (settings.Json)
			{
				outputService.WriteJsonError(errorMessage);
			}
			else
			{
				outputService.WriteMarkup($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' does not exist.");
			}

			return 1;
		}

		var successMessage = $"Default account set to '{settings.Name}'.";
		if (settings.Json)
		{
			outputService.WriteJson(new CommandResult(true, successMessage));
		}
		else
		{
			outputService.WriteSuccess($"Default account set to '[yellow]{Markup.Escape(settings.Name)}[/]'.");
		}

		return 0;
	}
}

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
			outputService.WriteMarkup($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' does not exist.");
			return 1;
		}

		outputService.WriteSuccess($"Account '[yellow]{Markup.Escape(settings.Name)}[/]' deleted successfully.");
		return 0;
	}
}

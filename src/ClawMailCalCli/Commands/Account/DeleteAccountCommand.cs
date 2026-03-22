using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Deletes an account from the store.
/// </summary>
internal sealed class DeleteAccountCommand(IAccountService accountService)
	: AsyncCommand<DeleteAccountCommand.Settings>
{
	/// <summary>
	/// Settings for the <see cref="DeleteAccountCommand"/>.
	/// </summary>
	internal sealed class Settings
		: CommandSettings
	{
		/// <summary>
		/// The name of the account to delete.
		/// </summary>
		[CommandArgument(0, "<name>")]
		public required string Name { get; init; }
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		var deleted = await accountService.DeleteAccountAsync(settings.Name, cancellationToken);
		if (!deleted)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' does not exist.");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' deleted successfully.");
		return 0;
	}
}

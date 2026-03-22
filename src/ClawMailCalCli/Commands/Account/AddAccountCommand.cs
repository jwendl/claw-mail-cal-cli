using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Adds a new account to the store.
/// </summary>
internal sealed class AddAccountCommand(IAccountService accountService)
	: AsyncCommand<AddAccountCommand.Settings>
{
	/// <summary>
	/// Settings for the <see cref="AddAccountCommand"/>.
	/// </summary>
	internal sealed class Settings
		: CommandSettings
	{
		/// <summary>
		/// The name of the account to add.
		/// </summary>
		[CommandArgument(0, "<name>")]
		public required string Name { get; init; }

		/// <summary>
		/// The email address for the account.
		/// </summary>
		[CommandArgument(1, "<email>")]
		public required string Email { get; init; }
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		var added = await accountService.AddAccountAsync(settings.Name, settings.Email, cancellationToken);
		if (!added)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' already exists.");
			return 1;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Account '[yellow]{Markup.Escape(settings.Name)}[/]' added successfully.");
		return 0;
	}
}

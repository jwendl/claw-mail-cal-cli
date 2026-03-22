using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Lists all stored accounts in a table.
/// </summary>
internal sealed class ListAccountsCommand(IAccountService accountService)
	: AsyncCommand
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
	{
		var accounts = await accountService.ListAccountsAsync(cancellationToken);
		if (accounts.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No accounts found.[/]");
			return 0;
		}

		var table = new Table();
		table.AddColumn("Name");
		table.AddColumn("Email");

		foreach (var account in accounts)
		{
			table.AddRow(account.Name, account.Email);
		}

		AnsiConsole.Write(table);
		return 0;
	}
}

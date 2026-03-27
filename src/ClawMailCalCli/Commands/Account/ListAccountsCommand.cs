using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Lists all stored accounts in a table.
/// </summary>
internal sealed class ListAccountsCommand(IAccountService accountService, IOutputService outputService)
	: AsyncCommand<ListAccountSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ListAccountSettings settings, CancellationToken cancellationToken)
	{
		var accounts = await accountService.ListAccountsAsync(cancellationToken);

		if (settings.Json)
		{
			outputService.WriteJson(accounts);
			return 0;
		}

		if (accounts.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No accounts found.[/]");
			return 0;
		}

		var table = new Table();
		table.AddColumn("Name");
		table.AddColumn("Email");
		table.AddColumn("Type");

		foreach (var account in accounts)
		{
			table.AddRow(new Text(account.Name), new Text(account.Email), new Text(account.Type.ToString()));
		}

		AnsiConsole.Write(table);
		return 0;
	}
}

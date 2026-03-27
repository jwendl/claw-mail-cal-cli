using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Account;

/// <summary>
/// Lists all stored accounts in a table.
/// </summary>
internal sealed class ListAccountsCommand(IAccountService accountService, IOutputService outputService)
	: AsyncCommand
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
	{
		var accounts = await accountService.ListAccountsAsync(cancellationToken);
		if (accounts.Count == 0)
		{
			outputService.WriteWarning("No accounts found.");
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

		outputService.WriteTable(table);
		return 0;
	}
}

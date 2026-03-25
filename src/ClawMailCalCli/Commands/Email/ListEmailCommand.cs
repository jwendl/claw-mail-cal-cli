using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Email;

/// <summary>
/// Lists the 20 most recent messages from the inbox or from a named folder.
/// Usage: <c>claw-mail-cal-cli email list [folder-name]</c>
/// </summary>
internal sealed class ListEmailCommand(IEmailService emailService, IOutputService outputService)
	: AsyncCommand<ListEmailSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ListEmailSettings settings, CancellationToken cancellationToken)
	{
		var emails = await emailService.GetEmailsAsync(settings.FolderName, cancellationToken);

		if (settings.Json)
		{
			outputService.WriteJson(emails);
			return 0;
		}

		var table = new Table();
		table.AddColumn("From");
		table.AddColumn("Subject");
		table.AddColumn("Date");
		table.AddColumn("Read");

		foreach (var email in emails)
		{
			var readIndicator = email.IsRead ? "[green]✓[/]" : "[bold]●[/]";
			table.AddRow(
				new Markup(Markup.Escape(email.From)),
				new Markup(Markup.Escape(email.Subject)),
				new Markup(email.ReceivedDateTime.ToLocalTime().ToString("MMM dd yyyy")),
				new Markup(readIndicator));
		}

		outputService.WriteTable(table);
		return 0;
	}
}

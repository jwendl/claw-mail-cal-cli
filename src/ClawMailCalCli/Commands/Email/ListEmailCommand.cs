using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Email;

/// <summary>
/// Lists the 20 most recent messages from the inbox or from a named folder.
/// Usage: <c>claw-mail-cal-cli email list [folder-name]</c>
/// </summary>
internal sealed class ListEmailCommand(IEmailService emailService)
	: AsyncCommand<ListEmailCommand.Settings>
{
	/// <summary>
	/// Settings (arguments and options) for the <see cref="ListEmailCommand"/>.
	/// </summary>
	internal sealed class Settings : CommandSettings
	{
		/// <summary>
		/// The optional folder name to list messages from. Defaults to the inbox when omitted.
		/// </summary>
		[CommandArgument(0, "[folder-name]")]
		public string? FolderName { get; set; }
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
	{
		var emails = await emailService.GetEmailsAsync(settings.FolderName, cancellationToken);

		if (emails.Count == 0)
		{
			if (string.IsNullOrWhiteSpace(settings.FolderName))
			{
				AnsiConsole.MarkupLine("[yellow]No messages found in the inbox.[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[yellow]No messages found in folder '[bold]{Markup.Escape(settings.FolderName)}[/]'.[/]");
			}

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

		AnsiConsole.Write(table);
		return 0;
	}
}

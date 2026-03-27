using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Email;

/// <summary>
/// Deletes an email message by subject or Graph message ID.
/// Usage: <c>claw-mail-cal-cli email delete &lt;account-name&gt; &lt;subject-or-id&gt; [--confirm]</c>
/// </summary>
internal sealed class DeleteEmailCommand(IEmailService emailService, IOutputService outputService)
	: AsyncCommand<DeleteEmailSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, DeleteEmailSettings settings, CancellationToken cancellationToken)
	{
		if (!settings.Confirm)
		{
			var confirmed = AnsiConsole.Confirm($"Are you sure you want to delete email matching '[bold]{Markup.Escape(settings.SubjectOrId)}[/]' from account '[bold]{Markup.Escape(settings.AccountName)}[/]'?", defaultValue: false);
			if (!confirmed)
			{
				AnsiConsole.MarkupLine("[yellow]Delete cancelled.[/]");
				return 0;
			}
		}

		var deleted = await emailService.DeleteEmailAsync(settings.AccountName, settings.SubjectOrId, cancellationToken);

		if (!deleted)
		{
			if (settings.Json)
			{
				outputService.WriteJson(new { deleted = false, subjectOrId = settings.SubjectOrId });
				return 1;
			}

			outputService.WriteError($"No message found matching: {settings.SubjectOrId}");
			return 1;
		}

		if (settings.Json)
		{
			outputService.WriteJson(new { deleted = true, subjectOrId = settings.SubjectOrId });
			return 0;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Email matching '[bold]{Markup.Escape(settings.SubjectOrId)}[/]' deleted successfully.");
		return 0;
	}
}

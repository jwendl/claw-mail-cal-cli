using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Calendar;

/// <summary>
/// Deletes a calendar event by title or Graph event ID.
/// Usage: <c>claw-mail-cal-cli calendar delete &lt;query&gt; [--account &lt;name&gt;] [--confirm]</c>
/// </summary>
internal sealed class DeleteCalendarCommand(ICalendarService calendarService, IConfigurationService configurationService, IOutputService outputService)
	: AsyncCommand<DeleteCalendarSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, DeleteCalendarSettings settings, CancellationToken cancellationToken)
	{
		var accountName = settings.AccountName;

		if (string.IsNullOrWhiteSpace(accountName))
		{
			try
			{
				var configuration = await configurationService.ReadConfigurationAsync();
				accountName = configuration.DefaultAccount;
			}
			catch (InvalidOperationException invalidOperationException)
			{
				outputService.WriteError($"Warning: Could not read default account from configuration: {invalidOperationException.Message}");
			}
		}

		if (string.IsNullOrWhiteSpace(accountName))
		{
			outputService.WriteError("Error: No account specified. Use --account to specify one or set a default with 'account set'.");
			return 1;
		}

		if (!settings.Confirm)
		{
			var confirmed = AnsiConsole.Confirm($"Are you sure you want to delete calendar event matching '[bold]{Markup.Escape(settings.Query)}[/]' from account '[bold]{Markup.Escape(accountName)}[/]'?", defaultValue: false);
			if (!confirmed)
			{
				AnsiConsole.MarkupLine("[yellow]Delete cancelled.[/]");
				return 0;
			}
		}

		var deleted = await calendarService.DeleteEventAsync(settings.Query, accountName, cancellationToken);

		if (!deleted)
		{
			if (settings.Json)
			{
				outputService.WriteJson(new { deleted = false, query = settings.Query });
				return 1;
			}

			outputService.WriteError($"No event found matching '{settings.Query}'.");
			return 1;
		}

		if (settings.Json)
		{
			outputService.WriteJson(new { deleted = true, query = settings.Query });
			return 0;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] Calendar event matching '[bold]{Markup.Escape(settings.Query)}[/]' deleted successfully.");
		return 0;
	}
}

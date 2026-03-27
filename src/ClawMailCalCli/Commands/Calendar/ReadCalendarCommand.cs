using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Calendar;

/// <summary>
/// Reads a calendar event by title or unique Graph event ID and prints its details to the console.
/// </summary>
internal sealed class ReadCalendarCommand(ICalendarService calendarService, IConfigurationService configurationService, IOutputService outputService)
	: AsyncCommand<ReadCalendarSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ReadCalendarSettings settings, CancellationToken cancellationToken)
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

		var calendarEvent = await calendarService.ReadEventAsync(settings.Query, accountName, cancellationToken);

		if (calendarEvent is null)
		{
			outputService.WriteError($"No event found matching '{settings.Query}'.");
			return 1;
		}

		if (settings.Json)
		{
			outputService.WriteJson(calendarEvent);
			return 0;
		}

		DisplayEvent(calendarEvent);
		return 0;
	}

	private void DisplayEvent(CalendarEvent calendarEvent)
	{
		outputService.WriteMarkup($"[bold]Title:[/]     {Markup.Escape(calendarEvent.Subject)}");
		outputService.WriteMarkup($"[bold]Start:[/]     {Markup.Escape(calendarEvent.Start ?? "(not set)")}");
		outputService.WriteMarkup($"[bold]End:[/]       {Markup.Escape(calendarEvent.End ?? "(not set)")}");
		outputService.WriteMarkup($"[bold]Location:[/]  {Markup.Escape(calendarEvent.Location ?? "(none)")}");
		outputService.WriteMarkup($"[bold]Organizer:[/] {Markup.Escape(calendarEvent.Organizer ?? "(unknown)")}");

		if (calendarEvent.Attendees.Count > 0)
		{
			outputService.WriteMarkup("[bold]Attendees:[/]");
			foreach (var attendee in calendarEvent.Attendees)
			{
				outputService.WriteMarkup($"  - {Markup.Escape(attendee)}");
			}
		}
		else
		{
			outputService.WriteMarkup("[bold]Attendees:[/] (none)");
		}

		if (!string.IsNullOrWhiteSpace(calendarEvent.Body))
		{
			outputService.WriteMarkup("[bold]Body:[/]");
			outputService.WriteMarkup(Markup.Escape(calendarEvent.Body));
		}
	}
}

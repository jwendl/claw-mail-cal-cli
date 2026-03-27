using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Calendar;

/// <summary>
/// Lists the next upcoming calendar events for the currently configured (or specified) account.
/// Usage: <c>claw-mail-cal-cli calendar list [--account &lt;name&gt;]</c>
/// </summary>
internal sealed class ListCalendarCommand(ICalendarService calendarService, IAccountService accountService, IOutputService outputService)
	: AsyncCommand<ListCalendarSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ListCalendarSettings settings, CancellationToken cancellationToken)
	{
		var accountName = settings.AccountName;

		if (!string.IsNullOrWhiteSpace(accountName))
		{
			var account = await accountService.GetAccountAsync(accountName, cancellationToken);
			if (account is null)
			{
				outputService.WriteError($"Error: Account '{accountName}' does not exist.");
				return 1;
			}
		}

		var events = await calendarService.GetUpcomingEventsAsync(accountName, cancellationToken);
		if (events is null)
		{
			return 1;
		}

		if (events.Count == 0)
		{
			if (settings.Json)
			{
				outputService.WriteJson(Array.Empty<CalendarEventSummary>());
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]No upcoming calendar events found in the next 30 days.[/]");
			}

			return 0;
		}

		if (settings.Json)
		{
			outputService.WriteJson(events);
			return 0;
		}

		var table = new Table();
		table.AddColumn("Title");
		table.AddColumn("Start");
		table.AddColumn("End");
		table.AddColumn("Location");

		foreach (var calendarEvent in events)
		{
			var startText = calendarEvent.IsAllDay ? "All Day" : calendarEvent.Start.LocalDateTime.ToString("MMM dd HH:mm");
			var endText = calendarEvent.IsAllDay ? "All Day" : calendarEvent.End.LocalDateTime.ToString("MMM dd HH:mm");
			var locationText = string.IsNullOrWhiteSpace(calendarEvent.Location) ? string.Empty : calendarEvent.Location;

			table.AddRow(
				new Text(calendarEvent.Title),
				new Text(startText),
				new Text(endText),
				new Text(locationText));
		}

		outputService.WriteTable(table);
		return 0;
	}
}

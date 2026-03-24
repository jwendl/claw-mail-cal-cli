using ClawMailCalCli.Commands.Settings;
using ClawMailCalCli.Configuration;
using ClawMailCalCli.Services;

namespace ClawMailCalCli.Commands.Calendar;

/// <summary>
/// Reads a calendar event by title or unique Graph event ID and prints its details to the console.
/// </summary>
internal sealed class ReadCalendarCommand(ICalendarService calendarService, IConfigurationService configurationService)
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
				AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not read default account from configuration: {Markup.Escape(invalidOperationException.Message)}");
			}
		}

		if (string.IsNullOrWhiteSpace(accountName))
		{
			AnsiConsole.MarkupLine("[red]Error:[/] No account specified. Use [bold]--account[/] to specify one or set a default with [bold]account set[/].");
			return 1;
		}

		var calendarEvent = await calendarService.ReadEventAsync(settings.Query, accountName, cancellationToken);

		if (calendarEvent is null)
		{
			AnsiConsole.MarkupLine($"[yellow]No event found[/] matching '[bold]{Markup.Escape(settings.Query)}[/]'.");
			return 0;
		}

		DisplayEvent(calendarEvent);
		return 0;
	}

	private static void DisplayEvent(CalendarEvent calendarEvent)
	{
		AnsiConsole.MarkupLine($"[bold]Title:[/]     {Markup.Escape(calendarEvent.Subject)}");
		AnsiConsole.MarkupLine($"[bold]Start:[/]     {Markup.Escape(calendarEvent.Start ?? "(not set)")}");
		AnsiConsole.MarkupLine($"[bold]End:[/]       {Markup.Escape(calendarEvent.End ?? "(not set)")}");
		AnsiConsole.MarkupLine($"[bold]Location:[/]  {Markup.Escape(calendarEvent.Location ?? "(none)")}");
		AnsiConsole.MarkupLine($"[bold]Organizer:[/] {Markup.Escape(calendarEvent.Organizer ?? "(unknown)")}");

		if (calendarEvent.Attendees.Count > 0)
		{
			AnsiConsole.MarkupLine("[bold]Attendees:[/]");
			foreach (var attendee in calendarEvent.Attendees)
			{
				AnsiConsole.MarkupLine($"  - {Markup.Escape(attendee)}");
			}
		}
		else
		{
			AnsiConsole.MarkupLine("[bold]Attendees:[/] (none)");
		}

		if (!string.IsNullOrWhiteSpace(calendarEvent.Body))
		{
			AnsiConsole.MarkupLine("[bold]Body:[/]");
			AnsiConsole.WriteLine(calendarEvent.Body);
		}
	}
}

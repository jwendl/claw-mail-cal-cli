using System.Globalization;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Commands.Calendar;

/// <summary>
/// Creates a new calendar event in the default account's primary calendar.
/// Usage: <c>claw-mail-cal-cli calendar create &lt;title&gt; &lt;start-date-time&gt; &lt;end-date-time&gt; &lt;content&gt;</c>
/// </summary>
internal sealed class CreateCalendarCommand(ICalendarService calendarService, IOutputService outputService)
	: AsyncCommand<CreateCalendarSettings>
{
	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, CreateCalendarSettings settings, CancellationToken cancellationToken)
	{
		if (!DateTimeOffset.TryParseExact(settings.StartDateTime, ["yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ssZ"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
		{
			var startErrorMessage = $"Invalid start date/time format '{settings.StartDateTime}'. Use ISO 8601 format, e.g. 2026-03-25T09:00:00";
			if (settings.Json)
			{
				outputService.WriteJsonError(startErrorMessage);
			}
			else
			{
				outputService.WriteMarkup($"[red]✗[/] Failed to create event: invalid start date/time format '{Markup.Escape(settings.StartDateTime)}'. Use ISO 8601 format, e.g. 2026-03-25T09:00:00");
			}

			return 1;
		}

		if (!DateTimeOffset.TryParseExact(settings.EndDateTime, ["yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ssZ"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
		{
			var endErrorMessage = $"Invalid end date/time format '{settings.EndDateTime}'. Use ISO 8601 format, e.g. 2026-03-25T09:30:00";
			if (settings.Json)
			{
				outputService.WriteJsonError(endErrorMessage);
			}
			else
			{
				outputService.WriteMarkup($"[red]✗[/] Failed to create event: invalid end date/time format '{Markup.Escape(settings.EndDateTime)}'. Use ISO 8601 format, e.g. 2026-03-25T09:30:00");
			}

			return 1;
		}

		if (parsedEnd <= parsedStart)
		{
			const string endBeforeStartErrorMessage = "End date/time must be after start date/time.";
			if (settings.Json)
			{
				outputService.WriteJsonError(endBeforeStartErrorMessage);
			}
			else
			{
				outputService.WriteMarkup("[red]✗[/] Failed to create event: end date/time must be after start date/time.");
			}

			return 1;
		}

		var eventId = await calendarService.CreateEventAsync(settings.Title, parsedStart, parsedEnd, settings.Content, cancellationToken);

		if (eventId is null)
		{
			const string createErrorMessage = "The operation did not complete successfully. See any previous error messages for more details.";
			if (settings.Json)
			{
				outputService.WriteJsonError(createErrorMessage);
			}
			else
			{
				outputService.WriteMarkup("[red]✗[/] Failed to create event: the operation did not complete successfully. See any previous error messages for more details.");
			}

			return 1;
		}

		var successMessage = $"Calendar event '{settings.Title}' created.";
		if (settings.Json)
		{
			outputService.WriteJson(new CommandResult(true, successMessage, eventId));
		}
		else
		{
			outputService.WriteSuccess($"Calendar event '{Markup.Escape(settings.Title)}' created (ID: {Markup.Escape(eventId)})");
		}

		return 0;
	}
}

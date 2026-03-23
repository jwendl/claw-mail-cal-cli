using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Retrieves upcoming calendar events from Microsoft Graph and maps them to
/// <see cref="CalendarEventSummary"/> instances for display.
/// </summary>
public class CalendarService(IGraphClientService graphClientService)
	: ICalendarService
{
	private const int EventCount = 20;
	private const int LookAheadDays = 30;

	/// <inheritdoc />
	public async Task<IReadOnlyList<CalendarEventSummary>?> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
	{
		var startDateTime = DateTimeOffset.UtcNow;
		var endDateTime = startDateTime.AddDays(LookAheadDays);

		var response = await graphClientService.GetCalendarViewAsync(
			startDateTime,
			endDateTime,
			EventCount,
			["subject", "start", "end", "location", "isAllDay"],
			cancellationToken);

		if (response is null)
		{
			return null;
		}

		var events = response.Value ?? [];
		return events
			.Select(calendarEvent =>
			{
				var title = calendarEvent.Subject ?? "(No Title)";
				var isAllDay = calendarEvent.IsAllDay ?? false;
				var location = calendarEvent.Location?.DisplayName;

				var start = ParseEventDateTime(calendarEvent.Start);
				var end = ParseEventDateTime(calendarEvent.End);

				return new CalendarEventSummary(title, start, end, isAllDay, location);
			})
			.OrderBy(calendarEventSummary => calendarEventSummary.Start)
			.ToList();
	}

	private static DateTimeOffset ParseEventDateTime(Microsoft.Graph.Models.DateTimeTimeZone? dateTimeTimeZone)
	{
		if (dateTimeTimeZone is null || string.IsNullOrWhiteSpace(dateTimeTimeZone.DateTime))
		{
			return DateTimeOffset.MinValue;
		}

		if (DateTime.TryParse(dateTimeTimeZone.DateTime, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
		{
			return new DateTimeOffset(parsed, TimeSpan.Zero);
		}

		return DateTimeOffset.MinValue;
	}
}

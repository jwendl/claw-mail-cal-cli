using System.Globalization;
using ClawMailCalCli.Models;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides business logic for calendar events — reading a single event by ID or subject
/// and listing upcoming calendar events via Microsoft Graph.
/// </summary>
public class CalendarService(ICalendarGraphService calendarGraphService, IGraphClientService graphClientService, ILogger<CalendarService> logger)
	: ICalendarService
{
	/// <summary>
	/// Queries whose length is at or above this threshold are treated as Graph event IDs.
	/// Graph event IDs are typically 150+ characters long.
	/// </summary>
	private const int EventIdMinimumLength = 50;

	private const int EventCount = 20;
	private const int LookAheadDays = 30;

	/// <inheritdoc />
	public async Task<CalendarEvent?> ReadEventAsync(string query, string accountName, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("ReadEventAsync called with an empty query for account '{AccountName}'.", accountName);
			}

			return null;
		}

		if (IsEventId(query))
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Query treated as event ID for account '{AccountName}'.", accountName);
			}

			return await calendarGraphService.GetEventByIdAsync(accountName, query, cancellationToken);
		}

		if (logger.IsEnabled(LogLevel.Debug))
		{
			logger.LogDebug("Query treated as title search for account '{AccountName}'.", accountName);
		}

		var events = await calendarGraphService.GetEventsBySubjectFilterAsync(accountName, query, cancellationToken);
		return events.Count > 0 ? events[0] : null;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<CalendarEventSummary>?> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
	{
		var startDateTime = DateTimeOffset.UtcNow;
		var endDateTime = startDateTime.AddDays(LookAheadDays);

		try
		{
			var response = await graphClientService.ExecuteWithRetryAsync(
				async graphClient => await graphClient.Me.CalendarView.GetAsync(config =>
				{
					config.QueryParameters.StartDateTime = startDateTime.UtcDateTime.ToString("o");
					config.QueryParameters.EndDateTime = endDateTime.UtcDateTime.ToString("o");
					config.QueryParameters.Top = EventCount;
					config.QueryParameters.Select = ["subject", "start", "end", "location", "isAllDay"];
					config.QueryParameters.Orderby = ["start/dateTime asc"];
				}, cancellationToken),
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
		catch (InvalidOperationException)
		{
			return null;
		}
		catch (ODataError odataError)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Microsoft Graph returned an error: {odataError.Error?.Message ?? odataError.Message}");
			return null;
		}
	}

	private static bool IsEventId(string query) => query.Length >= EventIdMinimumLength;

	/// <inheritdoc />
	public async Task<string?> CreateEventAsync(string title, string startDateTime, string endDateTime, string content, CancellationToken cancellationToken = default)
	{
		try
		{
			var newEvent = new Event
			{
				Subject = title,
				Start = new DateTimeTimeZone { DateTime = startDateTime, TimeZone = "UTC" },
				End = new DateTimeTimeZone { DateTime = endDateTime, TimeZone = "UTC" },
				Body = new ItemBody { ContentType = BodyType.Text, Content = content },
			};

			var createdEvent = await graphClientService.ExecuteWithRetryAsync(
				async graphClient => await graphClient.Me.Events.PostAsync(newEvent, cancellationToken: cancellationToken),
				cancellationToken);

			if (createdEvent is null)
			{
				if (logger.IsEnabled(LogLevel.Warning))
				{
					logger.LogWarning("Graph API returned a null response when creating calendar event '{Title}'.", title);
				}

				return null;
			}

			return createdEvent.Id;
		}
		catch (InvalidOperationException invalidOperationException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(invalidOperationException, "Could not create calendar event '{Title}': no default account configured.", title);
			}

			return null;
		}
		catch (ODataError odataError)
		{
			if (logger.IsEnabled(LogLevel.Error))
			{
				logger.LogError(odataError, "Graph API error while creating calendar event '{Title}'.", title);
			}

			return null;
		}
	}

	private static DateTimeOffset ParseEventDateTime(Microsoft.Graph.Models.DateTimeTimeZone? dateTimeTimeZone)
	{
		if (dateTimeTimeZone is null || string.IsNullOrWhiteSpace(dateTimeTimeZone.DateTime))
		{
			return DateTimeOffset.MinValue;
		}

		if (!DateTime.TryParse(dateTimeTimeZone.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
		{
			return DateTimeOffset.MinValue;
		}

		if (!string.IsNullOrWhiteSpace(dateTimeTimeZone.TimeZone))
		{
			try
			{
				var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(dateTimeTimeZone.TimeZone);
				var unspecifiedDateTime = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
				return new DateTimeOffset(unspecifiedDateTime, timeZoneInfo.GetUtcOffset(unspecifiedDateTime));
			}
			catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
			{
				// Unrecognized timezone — fall through and treat as UTC
			}
		}

		return new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc), TimeSpan.Zero);
	}
}

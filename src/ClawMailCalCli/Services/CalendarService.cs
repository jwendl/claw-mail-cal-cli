namespace ClawMailCalCli.Services;

/// <summary>
/// Provides business logic for reading calendar events.
/// Detects whether the given query is a Graph event ID or a subject search term
/// and delegates to <see cref="ICalendarGraphService"/> accordingly.
/// </summary>
public class CalendarService(ICalendarGraphService calendarGraphService, ILogger<CalendarService> logger)
	: ICalendarService
{
	/// <summary>
	/// Queries whose length is at or above this threshold are treated as Graph event IDs.
	/// Graph event IDs are typically 150+ characters long.
	/// </summary>
	private const int EventIdMinimumLength = 50;

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

	private static bool IsEventId(string query) => query.Length >= EventIdMinimumLength;
}

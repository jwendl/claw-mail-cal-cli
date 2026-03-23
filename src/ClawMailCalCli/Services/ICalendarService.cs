using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides business logic for reading calendar events from Microsoft Graph
/// and retrieving upcoming calendar events.
/// </summary>
public interface ICalendarService
{
	/// <summary>
	/// Reads a calendar event by title (partial, case-insensitive match) or by its unique Graph event ID.
	/// </summary>
	/// <param name="query">
	/// A search string: if it is 50 or more characters it is treated as a Graph event ID;
	/// otherwise it is used as a partial subject filter.
	/// </param>
	/// <param name="accountName">The account name whose credentials are used for the request.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>
	/// The first matching <see cref="CalendarEvent"/>, or <see langword="null"/> if no event was found.
	/// </returns>
	Task<CalendarEvent?> ReadEventAsync(string query, string accountName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the next upcoming calendar events starting from now, ordered by start date/time ascending.
	/// Returns <see langword="null"/> if the operation could not be completed (e.g., no default account set
	/// or authentication error).
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<IReadOnlyList<CalendarEventSummary>?> GetUpcomingEventsAsync(CancellationToken cancellationToken = default);
}

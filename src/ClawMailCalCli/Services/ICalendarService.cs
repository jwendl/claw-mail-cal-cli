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

	/// <summary>
	/// Creates a new calendar event in the default account's primary calendar.
	/// </summary>
	/// <param name="title">The subject of the event.</param>
	/// <param name="startDateTime">The event start date and time as a <see cref="DateTimeOffset"/>, preferably in UTC.</param>
	/// <param name="endDateTime">The event end date and time as a <see cref="DateTimeOffset"/>, preferably in UTC.</param>
	/// <param name="content">The plain-text body content of the event.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// The Graph event ID of the created event, or <see langword="null"/> if creation failed.
	/// </returns>
	Task<string?> CreateEventAsync(string title, DateTimeOffset startDateTime, DateTimeOffset endDateTime, string content, CancellationToken cancellationToken = default);
}

namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Abstracts Microsoft Graph calendar event API calls for testability.
/// </summary>
public interface ICalendarGraphService
{
	/// <summary>
	/// Retrieves a calendar event by its unique Graph event ID.
	/// </summary>
	/// <param name="accountName">The account name whose credentials are used for the request.</param>
	/// <param name="eventId">The unique Microsoft Graph event identifier.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>The matching <see cref="CalendarEvent"/>, or <see langword="null"/> if not found.</returns>
	Task<CalendarEvent?> GetEventByIdAsync(string accountName, string eventId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches for calendar events whose subject contains the given text (case-insensitive).
	/// </summary>
	/// <param name="accountName">The account name whose credentials are used for the request.</param>
	/// <param name="subject">The partial subject text to search for.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A list of matching <see cref="CalendarEvent"/> objects (may be empty).</returns>
	Task<IReadOnlyList<CalendarEvent>> GetEventsBySubjectFilterAsync(string accountName, string subject, CancellationToken cancellationToken = default);
}

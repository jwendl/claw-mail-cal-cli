using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides calendar event retrieval operations.
/// </summary>
public interface ICalendarService
{
	/// <summary>
	/// Returns the next upcoming calendar events starting from now, ordered by start date/time ascending.
	/// Returns <see langword="null"/> if the operation could not be completed (e.g., no default account set
	/// or authentication error).
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<IReadOnlyList<CalendarEventSummary>?> GetUpcomingEventsAsync(CancellationToken cancellationToken = default);
}

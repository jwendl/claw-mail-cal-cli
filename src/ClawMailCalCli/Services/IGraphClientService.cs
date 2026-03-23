using Microsoft.Graph.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides access to Microsoft Graph API operations, handling authentication and
/// 401 Unauthorized responses automatically.
/// </summary>
public interface IGraphClientService
{
	/// <summary>
	/// Returns the calendar view events between the specified date/time range for the
	/// currently authenticated default account. Returns <see langword="null"/> if the
	/// request could not be completed (e.g., no default account, not authenticated).
	/// </summary>
	/// <param name="startDateTime">Start of the calendar view window.</param>
	/// <param name="endDateTime">End of the calendar view window.</param>
	/// <param name="top">Maximum number of events to return.</param>
	/// <param name="select">Properties to include in each event.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<EventCollectionResponse?> GetCalendarViewAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, int top, string[] select, CancellationToken cancellationToken = default);
}

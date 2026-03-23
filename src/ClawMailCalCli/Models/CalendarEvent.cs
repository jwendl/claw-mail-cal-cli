namespace ClawMailCalCli.Models;

/// <summary>
/// Represents a calendar event retrieved from Microsoft Graph.
/// </summary>
/// <param name="Id">The unique Graph identifier of the event.</param>
/// <param name="Subject">The subject (title) of the event.</param>
/// <param name="Start">The start date and time of the event in ISO 8601 format.</param>
/// <param name="End">The end date and time of the event in ISO 8601 format.</param>
/// <param name="Location">The display name of the event location, if any.</param>
/// <param name="Organizer">The display name of the event organizer, if any.</param>
/// <param name="Attendees">The display names of all event attendees.</param>
/// <param name="Body">The plain-text body content of the event.</param>
public record CalendarEvent(
	string Id,
	string Subject,
	string? Start,
	string? End,
	string? Location,
	string? Organizer,
	IReadOnlyList<string> Attendees,
	string? Body
);

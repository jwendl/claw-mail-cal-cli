namespace ClawMailCalCli.Models;

/// <summary>
/// A lightweight summary of a calendar event used for display purposes.
/// </summary>
/// <param name="Title">The event subject/title.</param>
/// <param name="Start">The start date and time of the event.</param>
/// <param name="End">The end date and time of the event.</param>
/// <param name="IsAllDay">Whether the event spans the entire day.</param>
/// <param name="Location">The optional location of the event.</param>
public record CalendarEventSummary(
	string Title,
	DateTimeOffset Start,
	DateTimeOffset End,
	bool IsAllDay,
	string? Location);

namespace ClawMailCalCli.Commands.Settings;

/// <summary>
/// Settings for the <c>calendar create</c> command.
/// </summary>
internal sealed class CreateCalendarSettings
	: CommandSettings
{
	/// <summary>
	/// The title (subject) of the calendar event.
	/// </summary>
	[CommandArgument(0, "<title>")]
	public required string Title { get; init; }

	/// <summary>
	/// The event start date and time in ISO 8601 format (e.g., <c>2026-03-25T09:00:00</c>).
	/// </summary>
	[CommandArgument(1, "<start-date-time>")]
	public required string StartDateTime { get; init; }

	/// <summary>
	/// The event end date and time in ISO 8601 format (e.g., <c>2026-03-25T09:30:00</c>).
	/// </summary>
	[CommandArgument(2, "<end-date-time>")]
	public required string EndDateTime { get; init; }

	/// <summary>
	/// The plain-text body content of the calendar event.
	/// </summary>
	[CommandArgument(3, "<content>")]
	public required string Content { get; init; }
}

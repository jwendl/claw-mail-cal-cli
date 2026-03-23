namespace ClawMailCalCli.Models;

/// <summary>
/// Represents a read email message fetched from the Microsoft Graph API.
/// </summary>
public record EmailMessage(
	string Id,
	string Subject,
	string From,
	string To,
	DateTimeOffset? ReceivedDateTime,
	string Body);

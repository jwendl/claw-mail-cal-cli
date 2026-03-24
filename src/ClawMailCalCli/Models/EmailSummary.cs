namespace ClawMailCalCli.Models;

/// <summary>
/// Represents a summary of an email message retrieved from Microsoft Graph.
/// </summary>
/// <param name="From">The email address of the sender.</param>
/// <param name="Subject">The subject line of the message.</param>
/// <param name="ReceivedDateTime">The date and time the message was received.</param>
/// <param name="IsRead">Whether the message has been read.</param>
public record EmailSummary(string From, string Subject, DateTimeOffset ReceivedDateTime, bool IsRead);

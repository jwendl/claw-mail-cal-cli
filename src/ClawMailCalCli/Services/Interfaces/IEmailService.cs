using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services.Interfaces;

/// <summary>
/// Defines email operations against the Microsoft Graph API.
/// </summary>
public interface IEmailService
{
	/// <summary>
	/// Sends a plain-text email using the default account.
	/// Writes a descriptive error message to the console and returns <see langword="false"/> on failure.
	/// </summary>
	/// <param name="to">The recipient email address.</param>
	/// <param name="subject">The email subject.</param>
	/// <param name="content">The plain-text body of the email.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the email was sent successfully; otherwise <see langword="false"/>.</returns>
	Task<bool> SendEmailAsync(string to, string subject, string content, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns up to 20 of the most recent messages from the inbox (when <paramref name="folderName"/>
	/// is <see langword="null"/>) or from the named folder.
	/// </summary>
	/// <param name="folderName">
	/// The well-known or display name of the folder to query, or <see langword="null"/> to query
	/// the inbox.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	/// The email summaries, or an empty list if no default account is set or the folder is not found.
	/// </returns>
	Task<IReadOnlyList<EmailSummary>> GetEmailsAsync(string? folderName = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Reads a single email message by subject (partial, case-insensitive match) or
	/// by exact Graph message ID.
	/// </summary>
	/// <param name="accountName">The short name of the account to query.</param>
	/// <param name="subjectOrId">Subject search text or a Graph message ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The matched <see cref="EmailMessage"/>, or <see langword="null"/> if not found.</returns>
	Task<EmailMessage?> ReadEmailAsync(string accountName, string subjectOrId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an email message by subject (partial, case-insensitive match) or by exact Graph message ID.
	/// </summary>
	/// <param name="accountName">The short name of the account to query.</param>
	/// <param name="subjectOrId">Subject search text or a Graph message ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the email was deleted successfully; otherwise <see langword="false"/>.</returns>
	Task<bool> DeleteEmailAsync(string accountName, string subjectOrId, CancellationToken cancellationToken = default);
}

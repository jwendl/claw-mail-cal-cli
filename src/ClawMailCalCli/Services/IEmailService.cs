using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Defines email operations against the Microsoft Graph API.
/// </summary>
public interface IEmailService
{
	/// <summary>
	/// Reads a single email message by subject (partial, case-insensitive match) or
	/// by exact Graph message ID.
	/// </summary>
	/// <param name="accountName">The short name of the account to query.</param>
	/// <param name="subjectOrId">Subject search text or a Graph message ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The matched <see cref="EmailMessage"/>, or <see langword="null"/> if not found.</returns>
	Task<EmailMessage?> ReadEmailAsync(string accountName, string subjectOrId, CancellationToken cancellationToken = default);
}

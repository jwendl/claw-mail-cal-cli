using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides operations for listing email messages using the current default account.
/// </summary>
public interface IEmailService
{
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
}

using ClawMailCalCli.Models;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides authenticated access to Microsoft Graph email endpoints with built-in 401 retry
/// logic via the Azure Identity token-refresh pipeline.
/// </summary>
public interface IGraphClientService
{
	/// <summary>
	/// Retrieves the most recent messages from the inbox of the given account.
	/// </summary>
	/// <param name="accountName">The short name of the account whose inbox to query.</param>
	/// <param name="top">The maximum number of messages to return.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<IReadOnlyList<EmailSummary>> GetInboxMessagesAsync(string accountName, int top = 20, CancellationToken cancellationToken = default);

	/// <summary>
	/// Retrieves the most recent messages from the named mail folder of the given account.
	/// </summary>
	/// <param name="accountName">The short name of the account to query.</param>
	/// <param name="folderName">The well-known or display name of the folder (e.g. "inbox", "sentitems").</param>
	/// <param name="top">The maximum number of messages to return.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="InvalidOperationException">Thrown when the folder is not found.</exception>
	Task<IReadOnlyList<EmailSummary>> GetFolderMessagesAsync(string accountName, string folderName, int top = 20, CancellationToken cancellationToken = default);
}

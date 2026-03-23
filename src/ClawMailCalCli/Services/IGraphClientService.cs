using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides a <see cref="GraphServiceClient"/> authenticated for a named account.
/// </summary>
public interface IGraphClientService
{
	/// <summary>
	/// Returns a <see cref="GraphServiceClient"/> authenticated for the given account,
	/// re-acquiring tokens as needed.
	/// </summary>
	/// <param name="accountName">The short name of the account.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<GraphServiceClient> GetClientAsync(string accountName, CancellationToken cancellationToken = default);
}

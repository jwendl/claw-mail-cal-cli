using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides an authenticated <see cref="GraphServiceClient"/> for the default account.
/// </summary>
public interface IGraphClientService
{
	/// <summary>
	/// Returns an authenticated <see cref="GraphServiceClient"/> for the default account,
	/// or <see langword="null"/> if no default account is configured or the account has not
	/// been authenticated with the <c>login</c> command.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<GraphServiceClient?> GetClientForDefaultAccountAsync(CancellationToken cancellationToken = default);
}

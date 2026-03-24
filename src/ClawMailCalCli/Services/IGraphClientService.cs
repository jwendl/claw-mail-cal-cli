using ClawMailCalCli.Models;
using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides Microsoft Graph API operations with automatic 401 retry and re-authentication.
/// </summary>
public interface IGraphClientService
{
	/// <summary>
	/// Executes a Microsoft Graph API operation using the default account's cached credentials.
	/// If the operation fails with a 401 Unauthorized response, the login flow is automatically
	/// triggered and the operation is retried once.
	/// </summary>
	/// <typeparam name="T">The return type of the Graph operation.</typeparam>
	/// <param name="operation">A delegate that performs the Graph API call using the provided <see cref="GraphServiceClient"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The result of the Graph API operation.</returns>
	Task<T> ExecuteWithRetryAsync<T>(Func<GraphServiceClient, Task<T>> operation, CancellationToken cancellationToken = default);
}

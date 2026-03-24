using ClawMailCalCli.Models;
using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Creates a <see cref="GraphServiceClient"/> for a given account using the account's cached
/// <see cref="Azure.Identity.AuthenticationRecord"/> stored in Azure Key Vault.
/// Returns <see langword="null"/> when the account has no stored authentication record.
/// </summary>
public interface IGraphServiceClientBuilder
{
	/// <summary>
	/// Builds a <see cref="GraphServiceClient"/> for the given account, or returns
	/// <see langword="null"/> if the account has no cached authentication record.
	/// </summary>
	/// <param name="account">The account whose credentials are used.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<GraphServiceClient?> BuildAsync(Account account, CancellationToken cancellationToken = default);
}

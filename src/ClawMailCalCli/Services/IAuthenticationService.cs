using Azure.Identity;

namespace ClawMailCalCli.Services;

/// <summary>
/// Provides methods for authenticating CLI accounts using the Entra ID device code flow
/// and for managing cached <see cref="AuthenticationRecord"/> instances stored in Azure Key Vault.
/// </summary>
public interface IAuthenticationService
{
	/// <summary>
	/// Authenticates the given account using the Entra device code flow.
	/// If a cached <see cref="AuthenticationRecord"/> is found in Key Vault the interactive
	/// prompt is skipped; otherwise the user is guided through the device code flow.
	/// </summary>
	/// <param name="accountName">The short name of the account to authenticate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task AuthenticateAsync(string accountName, CancellationToken cancellationToken = default);
}

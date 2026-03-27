using Azure.Identity;

namespace ClawMailCalCli.Services.Interfaces;

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
	/// When <c>--non-interactive</c> is active and no cached record exists, the method
	/// returns <see langword="false"/> immediately instead of prompting for a device code.
	/// </summary>
	/// <param name="accountName">The short name of the account to authenticate.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="forceInteractive">
	/// When <see langword="true"/>, the device-code flow is always allowed regardless of
	/// the <c>--non-interactive</c> flag. Use this for the <c>login</c> command which is
	/// inherently interactive.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if authentication succeeded;
	/// <see langword="false"/> if authentication could not be performed or completed, for example
	/// due to a configuration or lookup error, or because interactive authentication was required
	/// but suppressed by <c>--non-interactive</c> and no cached <see cref="AuthenticationRecord"/> was available.
	/// </returns>
	Task<bool> AuthenticateAsync(string accountName, CancellationToken cancellationToken = default, bool forceInteractive = false);
}

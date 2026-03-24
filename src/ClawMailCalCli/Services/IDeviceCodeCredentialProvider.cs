using Azure.Identity;

namespace ClawMailCalCli.Services;

/// <summary>
/// Abstracts the creation of <see cref="DeviceCodeCredential"/> instances and the
/// interactive device-code authentication step so that unit tests can substitute a mock.
/// </summary>
public interface IDeviceCodeCredentialProvider
{
	/// <summary>
	/// Runs the device code authentication flow and returns the resulting
	/// <see cref="AuthenticationRecord"/> that can be cached for silent re-authentication.
	/// </summary>
	/// <param name="options">Configured credential options (ClientId, TenantId, callback, etc.).</param>
	/// <param name="scopes">The OAuth scopes to request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task<AuthenticationRecord> AuthenticateAsync(DeviceCodeCredentialOptions options, string[] scopes, CancellationToken cancellationToken = default);
}

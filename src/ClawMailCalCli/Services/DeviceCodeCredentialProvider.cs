using Azure.Core;
using Azure.Identity;

namespace ClawMailCalCli.Services;

/// <summary>
/// Default implementation of <see cref="IDeviceCodeCredentialProvider"/> that uses
/// the real <see cref="DeviceCodeCredential"/> from the Azure Identity library.
/// </summary>
public class DeviceCodeCredentialProvider
	: IDeviceCodeCredentialProvider
{
	/// <inheritdoc />
	public async Task<AuthenticationRecord> AuthenticateAsync(DeviceCodeCredentialOptions options, string[] scopes, CancellationToken cancellationToken = default)
	{
		var deviceCodeCredential = new DeviceCodeCredential(options);
		var tokenRequestContext = new TokenRequestContext(scopes);
		return await deviceCodeCredential.AuthenticateAsync(tokenRequestContext, cancellationToken);
	}
}

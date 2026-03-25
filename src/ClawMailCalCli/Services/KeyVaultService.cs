using Azure;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Azure Key Vault implementation of <see cref="IKeyVaultService"/>.
/// Accesses Key Vault using the <see cref="Azure.Identity.AzureCliCredential"/>
/// so the machine must have <c>az login</c> completed.
/// </summary>
public class KeyVaultService(SecretClient secretClient, ILogger<KeyVaultService> logger)
	: IKeyVaultService
{
	/// <inheritdoc />
	public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
	{
		if (logger.IsEnabled(LogLevel.Debug))
		{
			logger.LogDebug("Key Vault: reading secret '{SecretName}'.", secretName);
		}

		try
		{
			var response = await secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
			return response.Value.Value;
		}
		catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404)
		{
			return null;
		}
	}

	/// <inheritdoc />
	public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
	{
		if (logger.IsEnabled(LogLevel.Debug))
		{
			logger.LogDebug("Key Vault: writing secret '{SecretName}'.", secretName);
		}

		await secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
	}
}

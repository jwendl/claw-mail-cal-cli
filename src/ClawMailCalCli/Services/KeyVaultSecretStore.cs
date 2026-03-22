using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace ClawMailCalCli.Services;

/// <summary>
/// Azure Key Vault implementation of <see cref="ISecretStore"/>.
/// </summary>
public class KeyVaultSecretStore(SecretClient secretClient, ILogger<KeyVaultSecretStore> logger)
	: ISecretStore
{
	/// <inheritdoc />
	public async Task<string?> GetSecretValueAsync(string name, CancellationToken cancellationToken = default)
	{
		try
		{
			var response = await secretClient.GetSecretAsync(name, cancellationToken: cancellationToken);
			return response.Value.Value;
		}
		catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Secret '{Name}' not found in Key Vault.", name);
			}

			return null;
		}
	}

	/// <inheritdoc />
	public async Task SetSecretValueAsync(string name, string value, CancellationToken cancellationToken = default)
	{
		await secretClient.SetSecretAsync(name, value, cancellationToken);
	}

	/// <inheritdoc />
	public async Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
	{
		try
		{
			var deleteOperation = await secretClient.StartDeleteSecretAsync(name, cancellationToken);
			await deleteOperation.WaitForCompletionAsync(cancellationToken);

			try
			{
				await secretClient.PurgeDeletedSecretAsync(name, cancellationToken);
			}
			catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404 || requestFailedException.Status == 403)
			{
				if (logger.IsEnabled(LogLevel.Debug))
				{
					logger.LogDebug("Secret '{Name}' could not be purged (status {Status}). It may already be purged, not deleted, or purge is not permitted.", name, requestFailedException.Status);
				}
			}
		}
		catch (RequestFailedException requestFailedException) when (requestFailedException.Status == 404)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Secret '{Name}' not found when attempting to delete.", name);
			}
		}
	}
}

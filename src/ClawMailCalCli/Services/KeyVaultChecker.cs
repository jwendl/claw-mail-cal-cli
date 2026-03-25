using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Checks Azure Key Vault reachability by attempting to read a probe secret
/// using the current <see cref="AzureCliCredential"/>.
/// </summary>
public class KeyVaultChecker
	: IKeyVaultChecker
{
	/// <inheritdoc />
	public async Task<bool> IsReachableAsync(string vaultUri, CancellationToken cancellationToken = default)
	{
		try
		{
			var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions()
			{
				ExcludeBrokerCredential = true,
				ExcludeAzureDeveloperCliCredential = true,
				ExcludeEnvironmentCredential = true,
				ExcludeInteractiveBrowserCredential = true,
				ExcludeManagedIdentityCredential = true,
				ExcludeVisualStudioCredential = true,
				ExcludeVisualStudioCodeCredential = true,
				ExcludeWorkloadIdentityCredential = true,
				ExcludeAzurePowerShellCredential = true,
			};
			var defaultAzureCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);

			var client = new SecretClient(new Uri(vaultUri), defaultAzureCredential);
			await client.GetSecretAsync("doctor-probe", cancellationToken: cancellationToken);
			return true;
		}
		catch (RequestFailedException requestFailedException) when (requestFailedException.Status is 404 or 403)
		{
			// 404: vault is reachable, the probe secret simply does not exist.
			// 403: vault is reachable but this identity lacks Get permission.
			// Both cases mean the vault endpoint is accessible.
			return true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception)
		{
			return false;
		}
	}
}

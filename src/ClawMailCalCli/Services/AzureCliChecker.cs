using Azure.Core;
using Azure.Identity;
using ClawMailCalCli.Services.Interfaces;

namespace ClawMailCalCli.Services;

/// <summary>
/// Checks Azure CLI authentication by attempting to get an access token
/// using the <see cref="DefaultAzureCredential"/> with only Azure CLI credential enabled.
/// </summary>
public class AzureCliChecker
	: IAzureCliChecker
{
	/// <inheritdoc />
	public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
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
			var accessToken = await defaultAzureCredential.GetTokenAsync(new TokenRequestContext(["https://vault.azure.net/.default"]), cancellationToken);
			return !string.IsNullOrWhiteSpace(accessToken.Token);
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

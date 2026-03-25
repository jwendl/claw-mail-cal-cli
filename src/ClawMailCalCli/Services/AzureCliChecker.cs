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
			var azureCliCredential = new AzureCliCredential();
			var accessToken = await azureCliCredential.GetTokenAsync(new TokenRequestContext(["https://vault.azure.net/.default"]), cancellationToken);
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

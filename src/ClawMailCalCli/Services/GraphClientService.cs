using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Builds an authenticated <see cref="GraphServiceClient"/> using the stored
/// <see cref="AuthenticationRecord"/> for the default account.
/// </summary>
public class GraphClientService(IAccountService accountService, IKeyVaultService keyVaultService, IOptions<EntraOptions> entraOptions, ILogger<GraphClientService> logger)
	: IGraphClientService
{
	private static readonly string[] GraphScopes =
	[
		"https://graph.microsoft.com/Mail.Send",
	];

	/// <inheritdoc />
	public async Task<GraphServiceClient?> GetClientForDefaultAccountAsync(CancellationToken cancellationToken = default)
	{
		var defaultAccount = await accountService.GetDefaultAccountAsync(cancellationToken);
		if (defaultAccount is null)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("No default account is configured.");
			}

			return null;
		}

		var authRecord = await LoadAuthenticationRecordAsync(defaultAccount.Name, cancellationToken);
		if (authRecord is null)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("No authentication record found for account '{AccountName}'. User must run 'login' first.", defaultAccount.Name);
			}

			return null;
		}

		var options = entraOptions.Value;
		var tenantId = defaultAccount.Type == AccountType.Personal
			? options.PersonalTenantId
			: options.WorkTenantId;

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = options.ClientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
			AuthenticationRecord = authRecord,
			DeviceCodeCallback = (_, _) => Task.CompletedTask,
		};

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, GraphScopes);
	}

	private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string accountName, CancellationToken cancellationToken)
	{
		var secretName = $"auth-record-{accountName}";
		var secretValue = await keyVaultService.GetSecretAsync(secretName, cancellationToken);
		if (string.IsNullOrWhiteSpace(secretValue))
		{
			return null;
		}

		try
		{
			var recordBytes = Convert.FromBase64String(secretValue);
			using var memoryStream = new MemoryStream(recordBytes);
			return await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
		}
		catch (Exception exception)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(exception, "Failed to deserialize cached AuthenticationRecord for account '{AccountName}'.", accountName);
			}

			return null;
		}
	}
}

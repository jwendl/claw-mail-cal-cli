using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Creates authenticated <see cref="GraphServiceClient"/> instances by loading the stored
/// <see cref="AuthenticationRecord"/> for a named account from Key Vault.
/// </summary>
public class GraphClientService(IAccountService accountService, IKeyVaultService keyVaultService, IOptions<EntraOptions> entraOptions, ILogger<GraphClientService> logger)
	: IGraphClientService
{
	private static readonly string[] GraphScopes =
	[
		"https://graph.microsoft.com/Mail.Read",
	];

	private static string AuthRecordSecretName(string accountName)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
		return $"auth-record-{accountName}";
	}

	/// <inheritdoc />
	public async Task<GraphServiceClient> GetClientAsync(string accountName, CancellationToken cancellationToken = default)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			throw new InvalidOperationException($"Account '{accountName}' not found. Use 'account add' to create it first.");
		}

		var options = entraOptions.Value;
		var tenantId = account.Type == AccountType.Personal
			? options.PersonalTenantId
			: options.WorkTenantId;

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = options.ClientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
			DeviceCodeCallback = (deviceCodeInfo, _) =>
			{
				AnsiConsole.MarkupLine($"[bold]Re-authenticating account:[/] {accountName}");
				AnsiConsole.MarkupLine(deviceCodeInfo.Message);
				return Task.CompletedTask;
			},
		};

		var existingRecord = await LoadAuthenticationRecordAsync(accountName, cancellationToken);
		if (existingRecord is not null)
		{
			credentialOptions.AuthenticationRecord = existingRecord;

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Using cached AuthenticationRecord for account '{AccountName}'.", accountName);
			}
		}

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, GraphScopes);
	}

	private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string accountName, CancellationToken cancellationToken)
	{
		var secretValue = await keyVaultService.GetSecretAsync(AuthRecordSecretName(accountName), cancellationToken);
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

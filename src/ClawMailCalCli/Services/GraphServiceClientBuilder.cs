using Azure.Identity;
using ClawMailCalCli.Models;
using ClawMailCalCli.Services.Interfaces;
using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Builds a <see cref="GraphServiceClient"/> for a given account using its cached
/// <see cref="AuthenticationRecord"/> stored in Azure Key Vault.
/// </summary>
/// <remarks>
/// The Entra application client ID and tenant ID are read from Key Vault secrets named
/// <c>{account-type-prefix}-client-id</c> and <c>{account-type-prefix}-tenant-id</c>,
/// where the prefix is <c>hotmail</c> for personal accounts and <c>exchange</c> for
/// work/school accounts.
/// </remarks>
public class GraphServiceClientBuilder(IKeyVaultService keyVaultService, ILogger<GraphServiceClientBuilder> logger)
	: IGraphServiceClientBuilder
{
	private static readonly string[] GraphScopes =
	[
		"https://graph.microsoft.com/Mail.Read",
		"https://graph.microsoft.com/Mail.ReadWrite",
		"https://graph.microsoft.com/Mail.Send",
		"https://graph.microsoft.com/Calendars.Read",
		"https://graph.microsoft.com/Calendars.ReadWrite",
		"https://graph.microsoft.com/User.Read",
	];

	private static string AuthRecordSecretName(string accountName)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
		return $"auth-record-{accountName}";
	}

	/// <inheritdoc />
	public async Task<GraphServiceClient?> BuildAsync(Account account, CancellationToken cancellationToken = default)
	{
		var secretValue = await keyVaultService.GetSecretAsync(AuthRecordSecretName(account.Name), cancellationToken);
		if (string.IsNullOrWhiteSpace(secretValue))
		{
			return null;
		}

		AuthenticationRecord? authenticationRecord = null;
		try
		{
			var recordBytes = Convert.FromBase64String(secretValue);
			using var memoryStream = new MemoryStream(recordBytes);
			authenticationRecord = await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
		}
		catch (FormatException formatException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(formatException, "Cached AuthenticationRecord for account '{AccountName}' is not valid Base64.", account.Name);
			}

			return null;
		}
		catch (Exception deserializationException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(deserializationException, "Failed to deserialize cached AuthenticationRecord for account '{AccountName}'.", account.Name);
			}

			return null;
		}

		var prefix = AccountTypeKeyVaultPrefix(account.Type);
		var clientId = await keyVaultService.GetSecretAsync($"{prefix}-client-id", cancellationToken);
		var tenantId = await keyVaultService.GetSecretAsync($"{prefix}-tenant-id", cancellationToken);

		if (string.IsNullOrWhiteSpace(clientId))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("Key Vault secret '{Prefix}-client-id' is not set; cannot build GraphServiceClient for account '{AccountName}'.", prefix, account.Name);
			}

			return null;
		}

		if (string.IsNullOrWhiteSpace(tenantId))
		{
			tenantId = TenantDefaults.GetDefaultTenantId(account.Type);

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug("Key Vault secret '{Prefix}-tenant-id' not set. Using default tenant ID '{TenantId}' for account type {AccountType}.", prefix, tenantId, account.Type);
			}
		}

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = clientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = TokenCachePersistenceOptionsFactory.Create(),
			AuthenticationRecord = authenticationRecord,
		};

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, GraphScopes);
	}

	/// <summary>
	/// Returns the Key Vault secret name prefix for the given account type.
	/// Personal accounts use <c>hotmail</c>; work/school accounts use <c>exchange</c>.
	/// </summary>
	private static string AccountTypeKeyVaultPrefix(AccountType accountType) => accountType switch
	{
		AccountType.Personal => "hotmail",
		AccountType.Work => "exchange",
		_ => throw new InvalidOperationException($"Unknown account type: {accountType}"),
	};
}

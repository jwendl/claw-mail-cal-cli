using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace ClawMailCalCli.Services;

/// <summary>
/// Builds a <see cref="GraphServiceClient"/> for a given account using its cached
/// <see cref="AuthenticationRecord"/> stored in Azure Key Vault.
/// </summary>
public class GraphServiceClientBuilder(IKeyVaultService keyVaultService, IOptions<EntraOptions> entraOptions, ILogger<GraphServiceClientBuilder> logger)
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
			AuthenticationRecord = authenticationRecord,
		};

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, GraphScopes);
	}
}

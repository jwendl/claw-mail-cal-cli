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

	private static string AuthRecordSecretName(string accountName)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
		return $"auth-record-{accountName}";
	}

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
			DeviceCodeCallback = (deviceCodeInfo, _) =>
			{
				AnsiConsole.MarkupLine($"[bold]Re-authentication required for account '[yellow]{Markup.Escape(defaultAccount.Name)}[/]':[/]");
				AnsiConsole.MarkupLine(Markup.Escape(deviceCodeInfo.Message));
				return Task.CompletedTask;
			},
		};

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, GraphScopes);
	}

	private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string accountName, CancellationToken cancellationToken)
	{
		string secretName;
		try
		{
			secretName = AuthRecordSecretName(accountName);
		}
		catch (ArgumentException argumentException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(argumentException, "Account name '{AccountName}' is not valid for use in a Key Vault secret name.", accountName);
			}

			return null;
		}

		var secretValue = await keyVaultService.GetSecretAsync(secretName, cancellationToken);
		if (string.IsNullOrWhiteSpace(secretValue))
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("No authentication record found for account '{AccountName}'. Run 'login <account-name>' to authenticate.", accountName);
			}

			return null;
		}

		try
		{
			var recordBytes = Convert.FromBase64String(secretValue);
			using var memoryStream = new MemoryStream(recordBytes);
			return await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
		}
		catch (FormatException formatException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(formatException, "Cached authentication data for account '{AccountName}' is not valid Base64. User must re-authenticate.", accountName);
			}

			AnsiConsole.MarkupLine($"[yellow]Warning:[/] Cached authentication data for account '[bold]{Markup.Escape(accountName)}[/]' is invalid. Please re-authenticate by running '[bold]login {Markup.Escape(accountName)}[/]'.");
			return null;
		}
		catch (Exception deserializationException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(deserializationException, "Failed to deserialize cached AuthenticationRecord for account '{AccountName}'. User must re-authenticate.", accountName);
			}

			AnsiConsole.MarkupLine($"[yellow]Warning:[/] Cached authentication data for account '[bold]{Markup.Escape(accountName)}[/]' could not be loaded. Please re-authenticate by running '[bold]login {Markup.Escape(accountName)}[/]'.");
			return null;
		}
	}
}

using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

namespace ClawMailCalCli.Services;

/// <summary>
/// Implements <see cref="IGraphClientService"/> by creating an authenticated
/// <see cref="GraphServiceClient"/> from the cached <see cref="AuthenticationRecord"/>
/// stored in Azure Key Vault. The Azure Identity token-refresh pipeline handles 401
/// responses transparently, retrying with a fresh access token as needed.
/// </summary>
public class GraphClientService(IAccountService accountService, IKeyVaultService keyVaultService, IOptions<EntraOptions> entraOptions, ILogger<GraphClientService> logger)
	: IGraphClientService
{
	private static readonly string[] GraphScopes =
	[
		"https://graph.microsoft.com/Mail.Read",
	];

	private static readonly string[] MessageSelectFields =
	[
		"id",
		"subject",
		"from",
		"receivedDateTime",
		"isRead",
	];

	/// <inheritdoc />
	public async Task<IReadOnlyList<EmailSummary>> GetInboxMessagesAsync(string accountName, int top = 20, CancellationToken cancellationToken = default)
	{
		var graphClient = await CreateGraphClientAsync(accountName, cancellationToken);

		var messageCollection = await graphClient.Me.Messages.GetAsync(config =>
		{
			config.QueryParameters.Top = top;
			config.QueryParameters.Select = MessageSelectFields;
			config.QueryParameters.Orderby = ["receivedDateTime desc"];
		}, cancellationToken);

		return MapMessages(messageCollection?.Value);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<EmailSummary>> GetFolderMessagesAsync(string accountName, string folderName, int top = 20, CancellationToken cancellationToken = default)
	{
		var graphClient = await CreateGraphClientAsync(accountName, cancellationToken);

		try
		{
			var messageCollection = await graphClient.Me.MailFolders[folderName].Messages.GetAsync(config =>
			{
				config.QueryParameters.Top = top;
				config.QueryParameters.Select = MessageSelectFields;
				config.QueryParameters.Orderby = ["receivedDateTime desc"];
			}, cancellationToken);

			return MapMessages(messageCollection?.Value);
		}
		catch (ODataError odataError) when (odataError.ResponseStatusCode == 404)
		{
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug(odataError, "Folder '{FolderName}' not found for account '{AccountName}'.", folderName, accountName);
			}

			throw new InvalidOperationException($"Folder '{folderName}' was not found.", odataError);
		}
	}

	private async Task<GraphServiceClient> CreateGraphClientAsync(string accountName, CancellationToken cancellationToken)
	{
		var account = await accountService.GetAccountAsync(accountName, cancellationToken);
		if (account is null)
		{
			throw new InvalidOperationException($"Account '{accountName}' does not exist.");
		}

		var options = entraOptions.Value;
		var tenantId = account.Type == AccountType.Personal
			? options.PersonalTenantId
			: options.WorkTenantId;

		var authenticationRecord = await LoadAuthenticationRecordAsync(accountName, cancellationToken);
		if (authenticationRecord is null)
		{
			throw new InvalidOperationException($"Account '{accountName}' has no cached authentication record. Please run 'login {accountName}' first.");
		}

		var credentialOptions = new DeviceCodeCredentialOptions
		{
			AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
			ClientId = options.ClientId,
			TenantId = tenantId,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions(),
			AuthenticationRecord = authenticationRecord,
			// Silent re-authentication only: the user must run 'login' first to populate the auth record.
			// Graph calls should not trigger an interactive prompt.
			DeviceCodeCallback = (_, _) => Task.CompletedTask,
		};

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential, GraphScopes);
	}

	private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(string accountName, CancellationToken cancellationToken)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
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
		catch (Exception exception) when (exception is FormatException or System.Text.Json.JsonException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(exception, "Cached authentication record for account '{AccountName}' is corrupt. Re-authentication may be required.", accountName);
			}

			return null;
		}
	}

	private static IReadOnlyList<EmailSummary> MapMessages(IList<Microsoft.Graph.Models.Message>? messages)
	{
		if (messages is null)
		{
			return [];
		}

		return messages
			.Select(message => new EmailSummary(
				message.From?.EmailAddress?.Address ?? "(unknown)",
				message.Subject ?? "(no subject)",
				message.ReceivedDateTime ?? DateTimeOffset.MinValue,
				message.IsRead ?? false))
			.ToList();
	}
}

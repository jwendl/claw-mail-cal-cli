using Azure.Identity;
using ClawMailCalCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Me.CalendarView;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace ClawMailCalCli.Services;

/// <summary>
/// Builds and uses a <see cref="GraphServiceClient"/> for the currently authenticated default
/// account, handling 401 Unauthorized errors by prompting the user to re-authenticate.
/// </summary>
public class GraphClientService(IAccountService accountService, IKeyVaultService keyVaultService, IOptions<EntraOptions> entraOptions, ILogger<GraphClientService> logger)
	: IGraphClientService
{
	private static string AuthRecordSecretName(string accountName)
	{
		KeyVaultNameValidator.EnsureValid(accountName);
		return $"auth-record-{accountName}";
	}

	/// <inheritdoc />
	public async Task<EventCollectionResponse?> GetCalendarViewAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, int top, string[] select, CancellationToken cancellationToken = default)
	{
		var defaultAccount = await accountService.GetDefaultAccountAsync(cancellationToken);
		if (defaultAccount is null)
		{
			AnsiConsole.MarkupLine("[red]Error:[/] No default account is set. Use [bold]account set <name>[/] to configure one.");
			return null;
		}

		var graphClient = await BuildGraphClientAsync(defaultAccount, cancellationToken);
		if (graphClient is null)
		{
			return null;
		}

		try
		{
			return await graphClient.Me.CalendarView.GetAsync(config =>
			{
				config.QueryParameters.StartDateTime = startDateTime.UtcDateTime.ToString("o");
				config.QueryParameters.EndDateTime = endDateTime.UtcDateTime.ToString("o");
				config.QueryParameters.Top = top;
				config.QueryParameters.Select = select;
				config.QueryParameters.Orderby = ["start/dateTime asc"];
			}, cancellationToken);
		}
		catch (ApiException apiException) when (apiException.ResponseStatusCode == 401)
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Authentication failed for account '[bold]{defaultAccount.Name}[/]'. Run [bold]claw-mail-cal-cli login {defaultAccount.Name}[/] to re-authenticate.");

			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogDebug(apiException, "401 Unauthorized from Microsoft Graph for account '{AccountName}'.", defaultAccount.Name);
			}

			return null;
		}
		catch (ApiException apiException)
		{
			var errorMessage = apiException is ODataError odataError ? odataError.Error?.Message ?? apiException.Message : apiException.Message;
			AnsiConsole.MarkupLine($"[red]Error:[/] Microsoft Graph returned an error: {errorMessage}");

			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(apiException, "Graph API error for account '{AccountName}'.", defaultAccount.Name);
			}

			return null;
		}
	}

	private async Task<GraphServiceClient?> BuildGraphClientAsync(Account account, CancellationToken cancellationToken)
	{
		var secretValue = await keyVaultService.GetSecretAsync(AuthRecordSecretName(account.Name), cancellationToken);
		if (string.IsNullOrWhiteSpace(secretValue))
		{
			AnsiConsole.MarkupLine($"[red]Error:[/] Account '[bold]{account.Name}[/]' is not authenticated. Run [bold]claw-mail-cal-cli login {account.Name}[/] first.");
			return null;
		}

		AuthenticationRecord authenticationRecord;
		try
		{
			var recordBytes = Convert.FromBase64String(secretValue);
			using var memoryStream = new MemoryStream(recordBytes);
			authenticationRecord = await AuthenticationRecord.DeserializeAsync(memoryStream, cancellationToken);
		}
		catch (Exception deserializationException)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning(deserializationException, "Failed to deserialize cached AuthenticationRecord for account '{AccountName}'.", account.Name);
			}

			AnsiConsole.MarkupLine($"[red]Error:[/] Cached authentication data for account '[bold]{account.Name}[/]' is invalid. Run [bold]claw-mail-cal-cli login {account.Name}[/] to re-authenticate.");
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
			DeviceCodeCallback = (deviceCodeInfo, _) =>
			{
				AnsiConsole.WriteLine(deviceCodeInfo.Message);
				return Task.CompletedTask;
			},
		};

		var credential = new DeviceCodeCredential(credentialOptions);
		return new GraphServiceClient(credential);
	}
}
